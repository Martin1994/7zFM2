using System.CommandLine;
using SkiaSharp;
using Svg.Skia;

namespace SevenZip.FileManager2.Build;

/// <summary>Output profiles describe the consuming head, not the build host or target framework.</summary>
internal enum AppIconPlatform
{
    MacOS,
    Gtk,
    Windows
}

/// <summary>
/// Manually generates exactly one platform's app icon. No theme conversion or source modification.
/// </summary>
internal static class AppIconGenerator
{
    public static Command CreateCommand()
    {
        var platform = new Argument<AppIconPlatform>("platform") { Description = "macos (1024 PNG), gtk (512 PNG), or windows (multi-size ICO)." };
        var source = new Argument<string>("source")
        {
            Description = "Application SVG; defaults to Icons/AppIcons/icon2.svg.",
            Arity = ArgumentArity.ZeroOrOne,
            DefaultValueFactory = _ => ResourcePaths.AppIconSource
        };
        var output = new Argument<string>("output-directory")
        {
            Description = "Output directory; defaults to Icons/AppIcons.",
            Arity = ArgumentArity.ZeroOrOne,
            DefaultValueFactory = _ => ResourcePaths.AppIconsDirectory
        };
        var command = new Command("app-icons", "Generate one platform's original-color appicon-<platform> file.") { platform, source, output };
        command.SetAction(result =>
        {
            try
            {
                Generate(result.GetRequiredValue(source), result.GetRequiredValue(output), result.GetRequiredValue(platform));
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"AppIconGenerator: error: {exception.Message}");
                return 1;
            }
        });
        return command;
    }

    public static void Generate(string source, string outputDirectory, AppIconPlatform platform)
    {
        (string filename, int pixels) = platform switch
        {
            AppIconPlatform.MacOS => ("appicon-macos.png", 1024),
            AppIconPlatform.Gtk => ("appicon-gtk.png", 512),
            AppIconPlatform.Windows => ("appicon-windows.ico", 0),
            _ => throw new ArgumentOutOfRangeException(nameof(platform))
        };
        using var svg = new SKSvg();
        SKPicture picture = svg.Load(source)
            ?? throw new InvalidDataException($"Cannot load SVG: {source}");
        Directory.CreateDirectory(outputDirectory);
        string path = Path.Combine(outputDirectory, filename);
        if (platform == AppIconPlatform.Windows)
        {
            IcoWriter.Write(picture, path);
        }
        else
        {
            using SKImage image = SvgRasterizer.Render(picture, pixels);
            PngWriter.Write(image, path);
        }
        Console.WriteLine($"AppIcon: generated {Path.GetFullPath(path)}");
    }
}
