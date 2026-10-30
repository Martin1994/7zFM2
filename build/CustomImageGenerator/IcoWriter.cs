using System.Text;
using SkiaSharp;

namespace SevenZip.FileManager2.Build;

/// <summary>
/// Writes a Windows Vista+ ICO containing only this pipeline's 8-bit RGBA PNG frames.
/// Each size is rendered directly from SVG. PNG bytes are copied unchanged: no DIB header,
/// palette, AND/XOR mask, pixel conversion or Windows-specific API is involved.
/// This is not a general-purpose PNG/ICO importer; PNG encoding remains Skia's responsibility.
/// </summary>
/// <remarks>
/// Directory layout: https://devblogs.microsoft.com/oldnewthing/20101018-00/?p=12513
/// PNG payloads: https://devblogs.microsoft.com/oldnewthing/20101022-00/?p=12473
/// </remarks>
internal static class IcoWriter
{
    private const int HeaderSize = 6;
    private const int DirectoryEntrySize = 16;

    public static void Write(SKPicture picture, string path)
    {
        int[] sizes = [16, 24, 32, 48, 64, 128, 256];
        var frames = new List<Frame>(sizes.Length);
        foreach (int size in sizes)
        {
            using SKImage image = SvgRasterizer.Render(picture, size);
            using SKData png = image.Encode(SKEncodedImageFormat.Png, 100)
                ?? throw new InvalidOperationException("Cannot encode ICO frame.");
            frames.Add(new Frame(size, png.ToArray()));
        }
        GeneratedFile.Write(path, stream => WriteContainer(stream, frames));
    }

    // GeneratedFile supplies a new file starting at offset zero. Precomputed offsets allow
    // sequential writing without seeking; BinaryWriter writes integer fields little-endian.
    private static void WriteContainer(Stream stream, IReadOnlyList<Frame> frames)
    {
        if (frames.Count is < 1 or > ushort.MaxValue)
        {
            throw new InvalidDataException("ICO must contain between 1 and 65535 frames.");
        }
        uint offset = checked((uint)(HeaderSize + DirectoryEntrySize * frames.Count));
        uint totalLength = offset;
        var sizes = new HashSet<int>();
        foreach (Frame frame in frames)
        {
            if (frame.Size is < 1 or > 256 || frame.Png.Length == 0 || !sizes.Add(frame.Size))
            {
                throw new InvalidDataException("ICO frames must have unique sizes from 1 to 256 and nonempty PNG data.");
            }
            totalLength = checked(totalLength + (uint)frame.Png.Length);
        }

        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        writer.Write((ushort)0); // Reserved.
        writer.Write((ushort)1); // Image type: icon, not cursor.
        writer.Write((ushort)frames.Count);
        foreach (Frame frame in frames)
        {
            byte dimension = frame.Size == 256 ? (byte)0 : (byte)frame.Size;
            writer.Write(dimension); // Width; zero represents 256.
            writer.Write(dimension); // Height; PNG height is NOT doubled as in DIB icons.
            writer.Write((byte)0);   // No color palette.
            writer.Write((byte)0);   // Reserved.
            writer.Write((ushort)1); // Color planes.
            writer.Write((ushort)32); // RGBA, eight bits per channel.
            writer.Write((uint)frame.Png.Length);
            writer.Write(offset);   // Relative to the beginning of the ICO file.
            offset = checked(offset + (uint)frame.Png.Length);
        }
        foreach (Frame frame in frames)
        {
            writer.Write(frame.Png);
        }
    }

    private sealed record Frame(int Size, byte[] Png);
}
