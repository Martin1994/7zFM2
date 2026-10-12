using System.Diagnostics;
using Microsoft.Maui.Storage;

namespace SevenZip.FileManager2.Platform;

/// <summary>
/// Where an exception was captured.
/// </summary>
/// <remarks>
/// This ends up in every log record, so the names are the vocabulary a reader greps for. Keep them
/// stable.
/// </remarks>
public enum ExceptionSource
{
    /// <summary>Nobody caught it, on any thread. The process is on its way out.</summary>
    Unhandled,

    /// <summary>
    /// A <see cref="Task"/> whose exception was never observed. Raised by the GC, so it can arrive
    /// long after the mistake that caused it.
    /// </summary>
    UnobservedTask,

    /// <summary>It was on its way out of managed code, back into the platform (AppKit on macOS).</summary>
    NativeBoundary,
}

/// <summary>
/// One failure, as handed to a reporter.
/// </summary>
/// <param name="Source">Where it was captured.</param>
/// <param name="Exception">The failure itself, if there was one - an <c>AppDomain</c> hook can
/// deliver something that is not an <see cref="Exception"/>.</param>
/// <param name="Text">The formatted record: timestamp, source, note and stack. This is what gets
/// written to the console and the log file.</param>
public sealed record ExceptionReport(ExceptionSource Source, Exception? Exception, string Text);

/// <summary>
/// Central reporting for exceptions that nothing catches.
/// </summary>
/// <remarks>
/// <para>
/// .NET MAUI has no unhandled-exception API of its own - neither <c>Microsoft.Maui</c> nor the
/// macOS/Linux backends expose one - so these are the BCL hooks. Note what they can and cannot do:
/// <b>they report, they do not recover</b>. The process is on its way out either way -
/// <see cref="AppDomain.UnhandledException"/> cannot cancel termination, and
/// <see cref="TaskScheduler.UnobservedTaskException"/> can arrive minutes late, when the GC gets
/// round to collecting the faulted task. Recovery is only possible where the exception is caught,
/// which is why the risky call sites keep their own <c>try</c>/<c>catch</c>.
/// </para>
/// <para>
/// Recording - the debugger, the console, and the log file - is the same everywhere, so it lives
/// here and is the default. Presenting is not: this class stops at <see cref="Reporter"/>, and a head
/// supplies whatever its platform offers, typically an alert in the Win32 tradition: the user gets
/// to read the failure, and then the process goes away.
/// </para>
/// </remarks>
public static class UnhandledExceptions
{
    private const string LogFileName = "unhandled.log";

    /// <summary>
    /// Installation runs once, on first use. <see cref="Lazy{T}"/> rather than a check-then-set
    /// flag: the flag is a race, and a racing caller would return before the handlers are attached.
    /// </summary>
    private static readonly Lazy<bool> _installed = new(InstallHandlers);

    /// <summary>
    /// What happens to a report. <see cref="Record"/> is the default because it is platform-neutral;
    /// a head replaces this to present the failure as well, or instead.
    /// </summary>
    /// <remarks>
    /// Read when a report happens, so a head can set it as the first thing it does. A replacement
    /// should call <see cref="Record"/> first - that is the part that survives the process.
    ///
    /// Then think hard before blocking. Whether a failure is fatal is the wrong test: a hook can be
    /// running inside a native dispatch that a modal would re-enter. See the macOS head, where
    /// presenting from <see cref="ExceptionSource.NativeBoundary"/> was measured to hang the app.
    /// </remarks>
    public static Action<ExceptionReport> Reporter { get; set; } = static report => Record(report);

    /// <summary>
    /// Hooks <see cref="AppDomain.UnhandledException"/> and
    /// <see cref="TaskScheduler.UnobservedTaskException"/>. Safe to call more than once, and safe
    /// before the platform services exist: the log location is only resolved once something has
    /// actually gone wrong.
    /// </summary>
    public static void Install() => _ = _installed.Value;

    private static bool InstallHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Report(
            ExceptionSource.Unhandled,
            e.ExceptionObject as Exception,
            e.IsTerminating ? "terminating" : null);

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Report(ExceptionSource.UnobservedTask, e.Exception);

            // The event is raised once per task; marking it observed keeps a single mistake from
            // being reported over and over.
            e.SetObserved();
        };

        return true;
    }

    /// <summary>
    /// Records <paramref name="exception" />, caught at <paramref name="source" />, optionally with
    /// a <paramref name="note" /> qualifying it. Never throws.
    /// </summary>
    public static void Report(ExceptionSource source, Exception? exception, string? note = null)
    {
        var text =
            $"{DateTimeOffset.Now:O} {source}{(note is null ? "" : $" ({note})")}" +
            $"{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}";

        Reporter(new ExceptionReport(source, exception, text));
    }

    /// <summary>
    /// The platform-neutral half: the debugger, the console, and a log file next to the app's data.
    /// </summary>
    /// <returns>Where the report was written, or <c>null</c> if it could not be.</returns>
    /// <remarks>
    /// The file matters most where nothing else is watching: a bundle launched by <c>open</c> or
    /// Finder has detached stdout/stderr, and a Release build has no debugger at all - without the
    /// file those failures would be completely invisible.
    /// </remarks>
    public static string? Record(ExceptionReport report)
    {
        Debug.WriteLine(report.Text);
        Console.Error.Write(report.Text);

        try
        {
            // Resolved per report rather than at install time, because MAUI's platform services do
            // not exist yet that early.
            var path = Path.Combine(FileSystem.AppDataDirectory, LogFileName);

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.AppendAllText(path, report.Text);

            return path;
        }
        catch (Exception ex)
        {
            // Deliberately not silent. A report that cannot be kept is itself worth knowing about,
            // and the console half above has already happened.
            Console.Error.WriteLine($"UnhandledExceptions: cannot write {LogFileName}: {ex.Message}");

            return null;
        }
    }
}
