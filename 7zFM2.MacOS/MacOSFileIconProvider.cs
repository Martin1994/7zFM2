using AppKit;
using CoreGraphics;
using Microsoft.Maui.Dispatching;
using SevenZip.FileManager2.Icons;
using SevenZip.FileManager2.Platform;
using UniformTypeIdentifiers;

namespace SevenZip.FileManager2.MacOS;

/// <summary>Native AppKit workspace icons, never Quick Look thumbnails or file contents.</summary>
internal sealed class MacOSFileIconProvider : IFileIconProvider
{
    public string GetCacheKey(FileIconRequest request) => request.Kind switch
    {
        // Preserve path case: macOS volumes can be case-sensitive, and individual files can
        // have custom icons. Folder/drive requests deliberately use generic icons below.
        FileIconKind.File when !string.IsNullOrEmpty(request.FileSystemPath)
            => "macos:file:path:" + request.FileSystemPath,
        FileIconKind.File => "macos:file:extension:" + GetExtension(request.Name),
        FileIconKind.Directory => "macos:folder",
        FileIconKind.Drive => "macos:drive",
        FileIconKind.Computer => "macos:computer",
        _ => "macos:unknown",
    };

    public async Task<byte[]?> GetIconAsync(FileIconRequest request, int pixelSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pixelSize);
        if (App.Current is null)
        {
            return null;
        }

        try
        {
            // A cache may queue this call, and the AppKit dispatcher need not install a
            // SynchronizationContext. Keep *all* native work in this synchronous callback;
            // in particular, IconForFile must not be moved into Task.Run for filesystem I/O.
            return await App.Current.Dispatcher.DispatchAsync(() =>
            {
                var workspace = NSWorkspace.SharedWorkspace;
                var icon = request.Kind switch
                {
                    FileIconKind.File => GetFileIcon(workspace, request),
                    FileIconKind.Directory => workspace.GetIcon(UTTypes.Folder),
                    FileIconKind.Drive => workspace.GetIcon(UTTypes.Volume),
                    // AppKit's bound HFS icon overload supplies the system computer icon;
                    // there is no corresponding UTTypes.Computer or NSImageName.Computer.
                    FileIconKind.Computer => workspace.IconForFileType(HfsTypeCode.ComputerIcon),
                    _ => null,
                };

                return icon is null ? null : RenderPng(icon, pixelSize);
            }).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Icons are optional. Native lookup/encoding failures (or dispatcher shutdown)
            // must leave the caller's fallback usable rather than fail directory navigation.
            return null;
        }
    }

    private static string GetExtension(string name)
    {
        // Archive names can use either separator, independently of the host platform.
        var separator = name.LastIndexOfAny(['/', '\\']);
        var basename = name[(separator + 1)..];
        var dot = basename.LastIndexOf('.');
        return dot <= 0 || dot == basename.Length - 1
            ? string.Empty
            : basename[(dot + 1)..].ToLowerInvariant();
    }

    private static NSImage? GetFileIcon(NSWorkspace workspace, FileIconRequest request)
    {
        if (!string.IsNullOrEmpty(request.FileSystemPath))
        {
            // Do not probe/read the file or request a thumbnail. NSWorkspace handles paths,
            // including custom icons; virtual archive entries never enter this branch.
            var icon = workspace.IconForFile(request.FileSystemPath);
            if (icon is not null)
            {
                return icon;
            }
        }

        var extension = GetExtension(request.Name);
        if (extension.Length > 0)
        {
            using var type = UTType.CreateFromExtension(extension);
            if (type is not null)
            {
                var icon = workspace.GetIcon(type);
                if (icon is not null)
                {
                    return icon;
                }
            }
        }

        return workspace.GetIcon(UTTypes.Data);
    }

    private static byte[]? RenderPng(NSImage source, int pixelSize)
    {
        // NSWorkspace owns/caches its images. Do not dispose or change their Size/reps.
        // Drawing an owned copy also keeps any NSImage drawing cache private to this call.
        using var image = source.Copy() as NSImage;
        if (image is null)
        {
            return null;
        }

        var width = (double)image.Size.Width;
        var height = (double)image.Size.Height;
        if (!double.IsFinite(width) || !double.IsFinite(height) || width <= 0 || height <= 0)
        {
            return null;
        }

        // Explicit pixels and a 1:1 point size avoid the screen's Retina backing scale.
        // AppKit allocates/owns the interleaved, premultiplied RGBA buffer (null planes).
        using var bitmap = new NSBitmapImageRep(IntPtr.Zero, pixelSize, pixelSize,
            8, 4, true, false, NSColorSpace.DeviceRGB.ToString(), checked(pixelSize * 4), 32);
        bitmap.Size = new CGSize(pixelSize, pixelSize);
        using var context = NSGraphicsContext.FromBitmap(bitmap);
        if (context is null)
        {
            return null;
        }

        var bounds = new CGRect(0, 0, pixelSize, pixelSize);
        var scale = Math.Min(pixelSize / width, pixelSize / height);
        var target = new CGRect((pixelSize - width * scale) / 2,
            (pixelSize - height * scale) / 2, width * scale, height * scale);

        NSGraphicsContext.GlobalSaveGraphicsState();
        try
        {
            NSGraphicsContext.CurrentContext = context;
            context.ImageInterpolation = NSImageInterpolation.High;
            using var graphics = context.CGContext;
            graphics.ClearRect(bounds);
            image.Draw(target, new CGRect(0, 0, width, height), NSCompositingOperation.SourceOver, 1);
            graphics.Flush();
        }
        finally
        {
            NSGraphicsContext.GlobalRestoreGraphicsState();
        }

        using var data = bitmap.RepresentationUsingTypeProperties(NSBitmapImageFileType.Png);
        return data is null || data.Length == 0 ? null : data.ToArray();
    }
}