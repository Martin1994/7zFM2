using SevenZip.FileManager2.Icons;

namespace SevenZip.FileManager2.Controls;

/// <summary>Shared fallback asset data, independent of native lookups and their LRU cache.</summary>
internal static class DefaultFileIconSources
{
    private static readonly Lazy<byte[]> File = new(() => ReadAsset("default_file.png"));
    private static readonly Lazy<byte[]> Folder = new(() => ReadAsset("default_folder.png"));

    public static ImageSource ForKind(FileIconKind kind)
    {
        var asset = kind == FileIconKind.File ? File : Folder;
        // Share only the immutable bytes, not a View or an open stream. The stream constructor
        // wraps the array without copying it; each image load has its own position/lifetime.
        return ImageSource.FromStream(() => new MemoryStream(asset.Value, writable: false));
    }

    private static byte[] ReadAsset(string name)
    {
        using var input = typeof(DefaultFileIconSources).Assembly.GetManifestResourceStream(
            $"SevenZip.FileManager2.Icons.Assets.{name}")
            ?? throw new InvalidOperationException($"Missing default icon resource: {name}");
        using var output = new MemoryStream();
        input.CopyTo(output);
        return output.ToArray();
    }
}