using System.CommandLine;
using System.Globalization;
using System.Text.RegularExpressions;
using SkiaSharp;
using Svg.Skia;

namespace SevenZip.FileManager2.Build;

/// <summary>
/// Manually generates repository image resources; application builds never invoke this tool.
/// No arguments generate all themed PNGs and all three platform app icons.
/// The UTF-8 themed manifest contains scale followed by source|resource stem|logical size;
/// source paths resolve relative to the manifest. Generated binaries belong in version control.
/// </summary>
internal static partial class Program
{
    private static int Main(string[] args)
    {
        // dotnet run --project build/CustomImageGenerator
        // dotnet run --project build/CustomImageGenerator -- images [manifest] [output-directory]
        // dotnet run --project build/CustomImageGenerator -- app-icons gtk|macos|windows [source] [output-directory]
        var manifestArgument = new Argument<string>("manifest")
        {
            Description = "UTF-8 image manifest; defaults to Icons/Assets/icons.inputs.",
            Arity = ArgumentArity.ZeroOrOne,
            DefaultValueFactory = _ => ResourcePaths.Manifest
        };
        var outputDirectoryArgument = new Argument<string>("output-directory")
        {
            Description = "Dedicated themed PNG directory; defaults to Icons/Themed. Obsolete PNGs are removed.",
            Arity = ArgumentArity.ZeroOrOne,
            DefaultValueFactory = _ => ResourcePaths.ThemedDirectory
        };
        var command = new RootCommand("Manually generate version-controlled image resources. Defaults to all resources.");
        var images = new Command("images", "Generate original-color and lightness-inverted in-app PNGs.")
        {
            manifestArgument,
            outputDirectoryArgument
        };

        command.SetAction(_ => Run(() =>
        {
            Generate(ResourcePaths.Manifest, ResourcePaths.ThemedDirectory);
            foreach (AppIconPlatform platform in Enum.GetValues<AppIconPlatform>())
            {
                AppIconGenerator.Generate(ResourcePaths.AppIconSource, ResourcePaths.AppIconsDirectory, platform);
            }
        }));
        images.SetAction(result => Run(() => Generate(
            result.GetRequiredValue(manifestArgument), result.GetRequiredValue(outputDirectoryArgument))));
        command.Subcommands.Add(images);
        command.Subcommands.Add(AppIconGenerator.CreateCommand());
        return command.Parse(args).Invoke();
    }

    private static int Run(Action generate)
    {
        try
        {
            generate();
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"CustomImageGenerator: error: {exception.Message}");
            return 1;
        }
    }

    private static void Generate(string manifestPath, string outputDirectory)
    {
        string manifestDirectory = Path.GetDirectoryName(Path.GetFullPath(manifestPath))!;
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

            images.Add(new ImageInput(Path.GetFullPath(fields[0], manifestDirectory), fields[1], pixels));
        }

        Directory.CreateDirectory(outputDirectory);
        var outputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (ImageInput image in images)
        {
            using var svg = new SKSvg();
            SKPicture picture = svg.Load(image.Source)
                ?? throw new InvalidDataException($"Cannot load SVG: {image.Source}");
            using SKImage light = SvgRasterizer.Render(picture, image.Pixels);
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
        string filename = $"{resourceName}_{theme}.png";
        PngWriter.Write(image, Path.Combine(outputDirectory, filename));
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
