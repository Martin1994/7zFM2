using System.Drawing;
using System.Runtime.InteropServices;
using SevenZip.FileManager2.Icons;
using SevenZip.FileManager2.Platform;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace SevenZip.FileManager2.Windows;

/// <summary>Shell-associated icons (not thumbnails), with type-only lookups for archive entries.</summary>
internal sealed class WindowsFileIconProvider : IFileIconProvider
{
    private static string Extension(FileIconRequest request)
    {
        return Path.GetExtension(request.Name).ToLowerInvariant();
    }

    private static bool UsePath(FileIconRequest request)
    {
        if (request.FileSystemPath is null)
        {
            return false;
        }

        if (request.Kind == FileIconKind.Drive)
        {
            return true;
        }

        if (request.Kind != FileIconKind.File)
        {
            return false;
        }

        return Extension(request) is ".exe" or ".lnk" or ".ico" or ".url";
    }

    public string GetCacheKey(FileIconRequest request)
    {
        if (UsePath(request))
        {
            return "windows:path:" + request.FileSystemPath!.ToUpperInvariant();
        }

        var extension = string.Empty;
        if (request.Kind == FileIconKind.File)
        {
            extension = Extension(request);
        }

        return $"windows:type:{request.Kind}:{extension}";
    }

    public Task<byte[]?> GetIconAsync(FileIconRequest request, int pixelSize)
    {
        return Task.Run(() => LoadIcon(request, pixelSize));
    }

    private static byte[]? LoadIcon(FileIconRequest request, int pixelSize)
    {
        // SHGetFileInfo requires COM initialization. Pool threads are MTA; balance only a
        // successful initialization (including S_FALSE), never RPC_E_CHANGED_MODE.
        var initialized = CoInitializeEx(IntPtr.Zero, ComInitializationFlags.MultiThreaded) >= 0;
        try
        {
            var folder = request.Kind != FileIconKind.File;
            var attributes = folder ? FileAttributes.Directory : FileAttributes.Normal;
            var typeName = folder ? "folder" : "file" + Extension(request);
            var usePath = UsePath(request);
            var iconSize = pixelSize <= 16 ? ShellFileInfoFlags.SmallIcon : ShellFileInfoFlags.LargeIcon;
            var flags = ShellFileInfoFlags.Icon | iconSize;
            var path = typeName;

            if (usePath)
            {
                path = request.FileSystemPath!;
            }
            else
            {
                // Query associations from attributes without opening virtual/archive files.
                flags |= ShellFileInfoFlags.UseFileAttributes;
            }

            var info = new ShellFileInfo();
            var infoSize = (uint)Marshal.SizeOf<ShellFileInfo>();
            var result = SHGetFileInfo(path, attributes, ref info, infoSize, flags);

            if (result == IntPtr.Zero && usePath)
            {
                if (info.Icon != IntPtr.Zero)
                {
                    DestroyIcon(info.Icon);
                }

                info = new ShellFileInfo();
                result = SHGetFileInfo(
                    typeName,
                    attributes,
                    ref info,
                    infoSize,
                    flags | ShellFileInfoFlags.UseFileAttributes);
            }

            try
            {
                if (result == IntPtr.Zero || info.Icon == IntPtr.Zero)
                {
                    return null;
                }

                using var icon = Icon.FromHandle(info.Icon);
                using var bitmap = icon.ToBitmap();
                using var output = new MemoryStream();
                bitmap.Save(output, ImageFormat.Png);
                return output.ToArray();
            }
            finally
            {
                if (info.Icon != IntPtr.Zero)
                {
                    DestroyIcon(info.Icon);
                }
            }
        }
        finally
        {
            if (initialized)
            {
                CoUninitialize();
            }
        }
    }

    /// <summary>SHGFI flags used by this provider.</summary>
    [Flags]
    private enum ShellFileInfoFlags : uint
    {
        LargeIcon = 0x00000000,
        SmallIcon = 0x00000001,
        UseFileAttributes = 0x00000010,
        Icon = 0x00000100,
    }

    /// <summary>COINIT flags used by this provider.</summary>
    [Flags]
    private enum ComInitializationFlags : uint
    {
        MultiThreaded = 0x00000000,
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ShellFileInfo
    {
        public IntPtr Icon;
        public int IconIndex;
        public uint Attributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string DisplayName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string TypeName;
    }

    [DllImport("shell32.dll", EntryPoint = "SHGetFileInfoW", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(
        string path,
        FileAttributes attributes,
        ref ShellFileInfo info,
        uint size,
        ShellFileInfoFlags flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr icon);

    [DllImport("ole32.dll")]
    private static extern int CoInitializeEx(IntPtr reserved, ComInitializationFlags flags);

    [DllImport("ole32.dll")]
    private static extern void CoUninitialize();
}