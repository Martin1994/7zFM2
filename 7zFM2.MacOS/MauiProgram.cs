using AppKit;
using Foundation;
using Microsoft.Maui.Platforms.MacOS.Essentials;
using Microsoft.Maui.Platforms.MacOS.Hosting;
using ObjCRuntime;
using SevenZip.FileManager2.Platform;
using SevenZip.Native;

namespace SevenZip.FileManager2.MacOS;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // First statement, so that a failure during start-up - before there is even a window - is
        // already presented the way this head presents failures.
        UnhandledExceptions.Reporter = ReportAndPresent;

        // AppKit calls back into managed code with nothing above it to catch a failure, so an
        // exception raised inside one of those callbacks - a gesture recognizer, a notification
        // handler, a timer - has nowhere to land. This hook sees all of them.
        //
        // Reporting happens in every configuration: that record is the whole point, and a shipped
        // build needs it just as much as a debug one.
        //
        // Turning the exception into a crash does not. Fatal-by-default is a development posture -
        // while porting, a crash at the throw site is worth far more than a degraded window. In
        // Release the runtime's own marshalling is left alone, because losing the session to a
        // defect in the preview backend is not something a user should have to absorb. The one
        // exception in both configurations is such a defect, named by DescribeKnownBackendDefect;
        // nothing is silenced, it is reported by name.
        Runtime.MarshalManagedException += (_, args) =>
        {
            var defect = DescribeKnownBackendDefect(args.Exception);

            UnhandledExceptions.Report(
                ExceptionSource.NativeBoundary,
                args.Exception,
                defect is null ? null : $"suppressed backend defect: {defect}");

#if DEBUG
            if (defect is null)
            {
                args.ExceptionMode = MarshalManagedExceptionMode.UnwindNativeCode;
            }
#endif
        };

        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiAppMacOS<App>()
            .AddMacOSEssentials();

        App.ConfigureServices(builder.Services);
        builder.Services.AddSingleton<IDialogPresenter, MacOSDialogPresenter>();
        builder.Services.AddSingleton<IFileIconProvider, MacOSFileIconProvider>();
        builder.Services.AddSingleton<IPlatformWorkarounds, MacOSPlatformWorkarounds>();
        builder.Services.AddSingleton<Controls.IDataTablePlatformLayout, MacOSDataTablePlatformLayout>();

        Console.WriteLine($"7-Zip: {SevenZipLibrary.Initialize()}");

        return builder.Build();
    }

    /// <summary>
    /// Records the failure, and then - for the failures raised by the runtime with nothing else
    /// waiting on them - shows it in a modal alert before the process goes away.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the old Windows habit and it can be worth keeping: the user gets to read what
    /// happened instead of watching the app vanish, and dismissing the alert is what lets the process
    /// carry on out the door.
    /// </para>
    /// <para>
    /// But only <see cref="ExceptionSource.Unhandled"/> is presented, which is a retreat from the
    /// plausible-looking rule "present the fatal ones". Fatal is the wrong question - what matters is
    /// whether a native frame is waiting on the hook. <c>NativeBoundary</c> is fatal in Debug, but
    /// that hook runs *inside* AppKit's dispatch of a notification with the exception still in
    /// flight, and a modal there re-enters the very notification that threw: the
    /// "Window was already activated" defect fires on every activation, so the alert reappears inside
    /// itself and the app hangs instead of dying. Measured 2026-10-05.
    /// </para>
    /// <para>
    /// The <c>AppDomain</c> hook has no native frame waiting on it, so it is the one place this is
    /// safe - and if it turns out not to be, the process is already on its way out, so the worst case
    /// is a window the user has to force-quit.
    /// </para>
    /// </remarks>
    private static void ReportAndPresent(ExceptionReport report)
    {
        var logPath = UnhandledExceptions.Record(report);

        if (report.Source is not ExceptionSource.Unhandled)
        {
            return;
        }

        if (!NSThread.IsMain)
        {
            return;
        }

        using var alert = new NSAlert
        {
            AlertStyle = NSAlertStyle.Critical,
            MessageText = "7zFM2 has to close",
            InformativeText =
                $"{report.Exception?.Message ?? "An unknown failure occurred."}{Environment.NewLine}" +
                $"{Environment.NewLine}Details: {logPath ?? "see the console"}",
        };

        alert.AddButton("Close");
        alert.RunModal();
    }

    /// <summary>
    /// Names a known defect in the MAUI Labs AppKit backend that the app has to live with, or
    /// <c>null</c> when the exception came from our own code and should be fatal.
    /// </summary>
    /// <remarks>
    /// Matched on the message, because both failures are an <see cref="InvalidOperationException"/>
    /// thrown by MAUI itself and there is no distinct type to test for. Keep this list as short as it
    /// can possibly be, give every entry a reason and a way to tell when it can go, and delete
    /// entries once the backend is fixed.
    /// </remarks>
    private static string? DescribeKnownBackendDefect(Exception? exception)
    {
        return exception switch
        {
            // MacOSMauiApplication.CreatePlatformWindow activates the window, and then
            // ApplicationDidBecomeActive activates it again on every re-activation, so
            // Microsoft.Maui.Controls.Window throws "Window was already activated" every time the window
            // regains focus. MAUI guards against this on iOS (Issue21948) and Windows (Issue14142); the
            // Labs backend does not. Upstream fix: do not activate a window that is already activated.
            InvalidOperationException { Message: "Window was already activated" } =>
                "MacOSMauiApplication.ApplicationDidBecomeActive re-activates an active window",

            // AppKit closes the window as part of termination, and that close notification arrives after
            // MAUI has already disconnected the window handler. WindowHandler.OnWindowClosed then reads
            // VirtualView through the generic ElementHandler<,> getter, which throws for a disconnected
            // handler instead of returning null. Without this entry the app aborts every time it quits,
            // and macOS reports a crash for an entirely ordinary exit.
            //
            // Already fixed upstream, not yet published: platforms/MacOS/src/MacOS/Platform/
            // WindowCloseCoordinator.cs routes the close through IElementHandler.VirtualView, which is
            // nullable and safe for late native callbacks, and tests/MacOS.Tests/
            // WindowCloseCoordinatorTests.cs pins that behaviour. Delete this entry once a build carrying
            // it is published - check for the string "WindowCloseCoordinator" in
            // Microsoft.Maui.Platforms.MacOS.dll.
            InvalidOperationException { Message: "VirtualView cannot be null here" } =>
                "WindowHandler.OnWindowClosed reads VirtualView after the handler is disconnected",

            _ => null,
        };
    }
}
