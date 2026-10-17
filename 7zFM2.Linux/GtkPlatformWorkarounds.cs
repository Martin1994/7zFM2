using System.Runtime.CompilerServices;
using Microsoft.Maui.Platforms.Linux.Gtk4.Platform;
using SevenZip.FileManager2.Platform;
using GtkEntryHandler = Microsoft.Maui.Platforms.Linux.Gtk4.Handlers.EntryHandler;
using GtkWindowHandler = Microsoft.Maui.Platforms.Linux.Gtk4.Handlers.WindowHandler;

namespace SevenZip.FileManager2.Linux;

/// <summary>
/// The backend defects this head works around.
/// </summary>
internal sealed class GtkPlatformWorkarounds : IPlatformWorkarounds
{
    private static readonly ConditionalWeakTable<Page, object> MenuBarOwners = new();
    private static readonly ConditionalWeakTable<Entry, object> PathEntries = new();

    /// <summary>
    /// Forward the file manager page's menus through its Shell to the native window.
    /// </summary>
    /// <remarks>
    /// GTK4 0.1.0-preview.12.26474.1 maps only Window.Content.MenuBarItems, which is the
    /// empty Shell collection in this app. ShellHandler does not forward its page's menus.
    /// Append to Content so the Shell page handler and native root container already exist.
    /// Install this mapping before any window handlers are created, not when the page is
    /// constructed inside the first Content mapping. It also runs for replacement windows.
    /// Only registered pages are affected; weak keys do not retain closed pages/windows.
    /// This covers our single-page Shell and static menu, not general navigation or dynamic
    /// menu collections. Delete when the GTK backend forwards Shell page menus itself.
    /// </remarks>
    static GtkPlatformWorkarounds()
    {
        GtkWindowHandler.Mapper.AppendToMapping(nameof(IWindow.Content), (handler, window) =>
        {
            if (window.Content is Shell { CurrentPage: { } page } &&
                MenuBarOwners.TryGetValue(page, out _) &&
                handler.PlatformView is { } nativeWindow)
            {
                GtkMenuBarManager.ApplyToWindow(nativeWindow, page);
            }
        });

        GtkEntryHandler.Mapper.ModifyMapping(nameof(IEntry.Text), (handler, entry, originalMapping) =>
        {
            if (entry is Entry { IsReadOnly: true } pathEntry && PathEntries.TryGetValue(pathEntry, out _))
            {
                MapPathText(handler, entry);
            }
            else
            {
                originalMapping?.Invoke(handler, entry);
            }
        });
    }

    /// <summary>
    /// Do not feed GTK's intermediate text back into the read-only path entry.
    /// </summary>
    /// <remarks>
    /// In GTK4 0.1.0-preview.12.26474.1, EntryHandler.MapText calls SetText while its
    /// private OnChanged callback unconditionally writes native text back into IEntry.Text.
    /// Replacing a non-empty path can report an empty intermediate value, causing MAUI's
    /// property queue to oscillate even with a OneWay binding. Suppress connected changed
    /// handlers during the native write, not GTK's default signal handler or other signals.
    /// Only the registered read-only address bar is affected. Preserve pre-existing blocked
    /// handlers and restore exactly those we block, including when SetText throws.
    /// HandlerFind + HandlerBlock works on older GLib versions too (ID-only BlockMatched
    /// requires GLib 2.78). Delete when the backend guards programmatic text updates.
    /// </remarks>
    private static void MapPathText(GtkEntryHandler handler, IEntry entry)
    {
        var nativeEntry = handler.PlatformView;
        var text = entry.Text ?? string.Empty;
        if (nativeEntry is null || nativeEntry.GetText() == text)
        {
            return;
        }

        var blockedHandlers = new List<ulong>();
        try
        {
            ulong signalHandler;
            while ((signalHandler = GObject.Functions.SignalHandlerFind(
                nativeEntry, GObject.SignalMatchType.Id | GObject.SignalMatchType.Unblocked,
                Gtk.Entry.ChangedSignal.Id, default, null, 0, 0)) != 0)
            {
                blockedHandlers.Add(signalHandler);
                GObject.Functions.SignalHandlerBlock(nativeEntry, signalHandler);
            }

            nativeEntry.SetText(text);
        }
        finally
        {
            foreach (var signalHandler in blockedHandlers)
            {
                if (GObject.Functions.SignalHandlerIsConnected(nativeEntry, signalHandler))
                {
                    GObject.Functions.SignalHandlerUnblock(nativeEntry, signalHandler);
                }
            }
        }
    }

    public void OnPageCreated(Page page)
    {
        MenuBarOwners.GetOrCreateValue(page);
        if (page.FindByName<Entry>("PathEntry") is { } pathEntry)
        {
            PathEntries.GetOrCreateValue(pathEntry);
        }
    }
}
