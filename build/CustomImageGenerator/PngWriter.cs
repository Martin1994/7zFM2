using SkiaSharp;

namespace SevenZip.FileManager2.Build;

/// <summary>Encodes an image as PNG, replacing the destination only after encoding succeeds.</summary>
internal static class PngWriter
{
    public static void Write(SKImage image, string path)
    {
        using SKData png = image.Encode(SKEncodedImageFormat.Png, 100)
            ?? throw new InvalidOperationException("Cannot encode PNG.");
        GeneratedFile.Write(path, png.SaveTo);
    }
}
