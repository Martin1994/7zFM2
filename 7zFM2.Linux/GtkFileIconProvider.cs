using System.Runtime.InteropServices;
using Microsoft.Maui.Dispatching;
using SevenZip.FileManager2.Icons;
using SevenZip.FileManager2.Platform;

namespace SevenZip.FileManager2.Linux;

/// <summary>GIO file-type icons rendered through the active GTK display's icon theme.</summary>
internal sealed class GtkFileIconProvider : IFileIconProvider
{
    public string GetCacheKey(FileIconRequest request) => request.Kind switch
    {
        // Do not normalize case or consult the filesystem on the UI thread.
        FileIconKind.File when !string.IsNullOrEmpty(request.FileSystemPath)
            => "gtk:file:path:" + request.FileSystemPath,
        FileIconKind.File => "gtk:file:name:" + GetBasename(request.Name),
        FileIconKind.Directory => "gtk:folder",
        FileIconKind.Drive => "gtk:drive",
        FileIconKind.Computer => "gtk:computer",
        _ => "gtk:unknown",
    };

    public async Task<byte[]?> GetIconAsync(FileIconRequest request, int pixelSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pixelSize);

        // Capture before awaiting: the GTK dispatcher need not install a SynchronizationContext.
        var dispatcher = Microsoft.Maui.Controls.Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            return null;
        }

        var icons = request.Kind == FileIconKind.File
            ? await Task.Run(() => QueryFileIcons(request)).ConfigureAwait(false)
            : Array.Empty<string>();

        return await dispatcher.DispatchAsync(() => RenderIcon(request.Kind, icons, pixelSize))
            .ConfigureAwait(false);
    }

    private static string GetBasename(string name)
    {
        // Archive paths can use either separator, regardless of the host running this backend.
        var separator = name.LastIndexOfAny(['/', '\\']);
        return name[(separator + 1)..];
    }

    private static string[] QueryFileIcons(FileIconRequest request)
    {
        var icons = new List<string>();
        string? contentType = null;
        if (!string.IsNullOrEmpty(request.FileSystemPath))
        {
            try
            {
                using var file = Gio.FileHelper.NewForPath(request.FileSystemPath);
                using var info = file.QueryInfo("standard::icon,standard::content-type",
                    Gio.FileQueryInfoFlags.None, null);
                using var icon = info.GetIcon();
                AddIcon(icons, icon);
                contentType = info.GetContentType();
            }
            catch (GLib.GException)
            {
                // Missing, inaccessible or concurrently removed file: use its name/type instead.
            }
        }

        try
        {
            if (string.IsNullOrEmpty(contentType))
            {
                // No file contents, thumbnails or archive extraction; keep README/Makefile names.
                contentType = Gio.Functions.ContentTypeGuess(GetBasename(request.Name),
                    Span<byte>.Empty, out _);
            }

            if (!string.IsNullOrEmpty(contentType))
            {
                using var icon = Gio.Functions.ContentTypeGetIcon(contentType);
                AddIcon(icons, icon);
                using var symbolic = Gio.Functions.ContentTypeGetSymbolicIcon(contentType);
                AddIcon(icons, symbolic);
            }
        }
        catch (GLib.GException)
        {
            // The UI-side generic theme fallback remains available.
        }

        return icons.ToArray();
    }

    private static void AddIcon(List<string> icons, Gio.Icon? icon)
    {
        // GIcon's serialization preserves themed fallback names and file/emblemed icons.
        // Only managed strings cross the worker/UI boundary; all GObjects are disposed locally.
        var serialized = icon?.ToString();
        if (!string.IsNullOrEmpty(serialized) && !icons.Contains(serialized))
        {
            icons.Add(serialized);
        }
    }

    private static byte[]? RenderIcon(FileIconKind kind, string[] icons, int pixelSize)
    {
        using var display = Gdk.Display.GetDefault();
        if (display is null)
        {
            return null;
        }

        using var theme = Gtk.IconTheme.GetForDisplay(display);
        foreach (var serialized in icons)
        {
            try
            {
                using var icon = Gio.IconHelper.NewForString(serialized);
                foreach (var flags in new[] { Gtk.IconLookupFlags.ForceRegular, Gtk.IconLookupFlags.None })
                {
                    using var paintable = theme.LookupByGicon(icon, pixelSize, 1,
                        Gtk.TextDirection.None, flags);
                    // GTK returns image-missing for an unresolved GIcon; try its type first.
                    if (paintable.GetIconName() == "image-missing")
                    {
                        continue;
                    }

                    if (TryEncode(paintable, pixelSize) is { } png)
                    {
                        return png;
                    }
                }
            }
            catch (GLib.GException)
            {
                // Unserializable/unavailable custom icon: continue with type/generic icons.
            }
        }

        string[] fallbacks = kind switch
        {
            FileIconKind.Directory => ["folder", "folder-symbolic"],
            FileIconKind.Drive => ["drive-harddisk", "drive-removable-media", "drive-harddisk-symbolic"],
            FileIconKind.Computer => ["computer", "computer-symbolic"],
            _ => ["text-x-generic", "application-octet-stream", "text-x-generic-symbolic"],
        };

        foreach (var name in fallbacks.Append("image-missing"))
        {
            try
            {
                if (!theme.HasIcon(name))
                {
                    continue;
                }

                using var paintable = theme.LookupIcon(name, null, pixelSize, 1,
                    Gtk.TextDirection.None, Gtk.IconLookupFlags.None);
                if (TryEncode(paintable, pixelSize) is { } png)
                {
                    return png;
                }
            }
            catch (GLib.GException)
            {
                // Broken theme entry or unavailable image loader: try the next fallback.
            }
        }

        return null;
    }

    private static byte[]? TryEncode(Gtk.IconPaintable paintable, int pixelSize)
    {
        try
        {
            using var file = paintable.GetFile();
            if (file is null)
            {
                return null;
            }

            // This is the icon asset selected by GTK, never the item's image contents.
            // GIO streams also support resource:// icons bundled with GTK/the theme.
            using var input = file.Read(null);
            using var pixbuf = GdkPixbuf.Pixbuf.NewFromStreamAtScale(input,
                pixelSize, pixelSize, true, null);
            if (pixbuf is null)
            {
                return null;
            }

            using var output = Gio.MemoryOutputStream.NewResizable();
            // GirCore 0.7 has no public Pixbuf.SaveToBufferv wrapper. The supported stream
            // binding avoids its internal byte[]/native-allocation marshalling ambiguity.
            if (!pixbuf.SaveToStreamv(output, "png", null, null, null) || !output.Close(null))
            {
                return null;
            }

            var length = checked((int)output.GetDataSize());
            if (length == 0)
            {
                return null;
            }

            var png = new byte[length];
            Marshal.Copy(output.GetData(), png, 0, length);
            return png;
        }
        catch (GLib.GException)
        {
            // Symbolic SVGs require the host's SVG pixbuf loader. They retain source colors:
            // this asset-loading path does not implement GTK CSS symbolic recoloring/emblems.
            return null;
        }
    }
}