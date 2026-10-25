using System.CommandLine;
using System.Globalization;
using System.Text.RegularExpressions;
using SkiaSharp;
using Svg.Skia;

namespace SevenZip.FileManager2.Build;

/// <summary>
/// Rasterizes CustomImage items, preserving source colors for light mode and inverting
/// HSL lightness for dark mode. The UTF-8 manifest contains the scale followed by
/// source|resource stem|logical size lines.
/// Sources are never modified. The output directory must be dedicated to generated images.
/// </summary>
internal static partial class Program
{
    private static int Main(string[] args)
    {
        var manifestArgument = new Argument<string>("manifest")
        {
            Description = "Path to the UTF-8 CustomImage manifest.",
            Arity = ArgumentArity.ExactlyOne
        };
        var outputDirectoryArgument = new Argument<string>("output-directory")
        {
            Description = "Directory dedicated to generated PNGs; obsolete PNGs will be removed.",
            Arity = ArgumentArity.ExactlyOne
        };
        var command = new RootCommand("Generate original-color and lightness-inverted high-resolution PNGs from SVGs.")
        {
            manifestArgument,
            outputDirectoryArgument
        };

        command.SetAction(parseResult =>
        {
            try
            {
                Generate(
                    parseResult.GetRequiredValue(manifestArgument),
                    parseResult.GetRequiredValue(outputDirectoryArgument));
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"CustomImageGenerator: error: {exception.Message}");
                return 1;
            }
        });

        return command.Parse(args).Invoke();
    }

    private static void Generate(string manifestPath, string outputDirectory)
    {
        string[] lines = File.ReadAllLines(manifestPath);
        if (lines.Length < 2)
        {
            throw new InvalidDataException("The CustomImage manifest must contain settings and at least one image.");
        }

        int scale = ReadPositiveInteger(lines[0]);
        var images = new List<ImageInput>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string line in lines.Skip(1))
        {
            string[] fields = line.Split('|');
            if (fields.Length != 3 || !ResourceNamePattern().IsMatch(fields[1]))
            {
                throw new InvalidDataException($"Invalid CustomImage entry: {line}");
            }

            if (!names.Add(fields[1]))
            {
                throw new InvalidDataException($"Duplicate CustomImage resource name: {fields[1]}");
            }

            int pixels = checked(ReadPositiveInteger(fields[2]) * scale);
            if (pixels > 4096)
            {
                throw new InvalidDataException($"CustomImage is larger than 4096 pixels: {fields[1]}");
            }

            images.Add(new ImageInput(fields[0], fields[1], pixels));
        }

        Directory.CreateDirectory(outputDirectory);
        var outputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (ImageInput image in images)
        {
            using var svg = new SKSvg();
            SKPicture picture = svg.Load(image.Source)
                ?? throw new InvalidDataException($"Cannot load SVG: {image.Source}");
            SKRect bounds = picture.CullRect;
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                throw new InvalidDataException($"SVG has empty bounds: {image.Source}");
            }

            // Render at the final pixel size, preserving aspect ratio and transparent padding.
            var info = new SKImageInfo(image.Pixels, image.Pixels, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var surface = SKSurface.Create(info)
                ?? throw new InvalidOperationException("Cannot allocate image surface.");
            surface.Canvas.Clear(SKColors.Transparent);
            float factor = Math.Min(image.Pixels / bounds.Width, image.Pixels / bounds.Height);
            surface.Canvas.Translate(
                (image.Pixels - bounds.Width * factor) / 2,
                (image.Pixels - bounds.Height * factor) / 2);
            surface.Canvas.Scale(factor);
            surface.Canvas.Translate(-bounds.Left, -bounds.Top);
            surface.Canvas.DrawPicture(picture);
            using SKImage light = surface.Snapshot();
            using SKImage dark = InvertLightness(light);
            outputs.Add(WritePng(light, outputDirectory, image.ResourceName, "light"));
            outputs.Add(WritePng(dark, outputDirectory, image.ResourceName, "dark"));
        }

        // Only our dedicated output directory is cleaned; never touch upstream SVGs.
        foreach (string oldOutput in Directory.EnumerateFiles(outputDirectory, "*.png"))
        {
            if (!outputs.Contains(Path.GetFileName(oldOutput)))
            {
                File.Delete(oldOutput);
            }
        }

        Console.WriteLine($"CustomImage: generated {outputs.Count} PNGs at {scale}x in {outputDirectory}");
    }

    /// <summary>
    /// Applies HSL L' = 1 - L to unassociated RGB, not to premultiplied channels.
    /// Adding 255 - max(R,G,B) - min(R,G,B) to every channel preserves hue and
    /// saturation exactly in 8-bit RGB; alpha, including edge coverage, is unchanged.
    /// This is encoded-RGB HSL lightness, not linear-light physical luminance.
    /// </summary>
    private static SKImage InvertLightness(SKImage source)
    {
        var info = new SKImageInfo(source.Width, source.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var bitmap = new SKBitmap(info);
        if (!source.ReadPixels(info, bitmap.GetPixels(), bitmap.RowBytes, 0, 0))
        {
            throw new InvalidOperationException("Cannot read image pixels for lightness inversion.");
        }

        SKColor[] pixels = bitmap.Pixels;
        for (int index = 0; index < pixels.Length; index++)
        {
            SKColor color = pixels[index];

            int maximum = Math.Max(color.Red, Math.Max(color.Green, color.Blue));
            int minimum = Math.Min(color.Red, Math.Min(color.Green, color.Blue));
            int offset = 255 - maximum - minimum;
            pixels[index] = new SKColor(
                (byte)(color.Red + offset),
                (byte)(color.Green + offset),
                (byte)(color.Blue + offset),
                color.Alpha);
        }

        bitmap.Pixels = pixels;
        return SKImage.FromBitmap(bitmap);
    }

    private static string WritePng(SKImage image, string outputDirectory, string resourceName, string theme)
    {
        using SKData png = image.Encode(SKEncodedImageFormat.Png, 100)
            ?? throw new InvalidOperationException("Cannot encode PNG.");
        string filename = $"{resourceName}_{theme}.png";
        string output = Path.Combine(outputDirectory, filename);
        string temporary = output + ".tmp";
        try
        {
            using (FileStream stream = File.Create(temporary))
            {
                png.SaveTo(stream);
            }

            File.Move(temporary, output, overwrite: true);
        }
        finally
        {
            File.Delete(temporary);
        }

        return filename;
    }

    private static int ReadPositiveInteger(string value)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int result) || result <= 0)
        {
            throw new InvalidDataException($"Expected a positive integer, got '{value}'.");
        }

        return result;
    }

    [GeneratedRegex("^[a-z][a-z0-9_]*[a-z]$")]
    private static partial Regex ResourceNamePattern();

    private sealed record ImageInput(string Source, string ResourceName, int Pixels);
}