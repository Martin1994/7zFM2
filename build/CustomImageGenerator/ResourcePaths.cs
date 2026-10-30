using System.Runtime.CompilerServices;

namespace SevenZip.FileManager2.Build;

/// <summary>Default repository locations for manual generation, independent of the shell's working directory.</summary>
internal static class ResourcePaths
{
    public static string IconsDirectory { get; } = FindIconsDirectory();
    public static string Manifest => Path.Combine(IconsDirectory, "Assets", "icons.inputs");
    public static string ThemedDirectory => Path.Combine(IconsDirectory, "Themed");
    public static string AppIconsDirectory => Path.Combine(IconsDirectory, "AppIcons");
    public static string AppIconSource => Path.Combine(AppIconsDirectory, "icon2.svg");

    private static string FindIconsDirectory([CallerFilePath] string sourceFile = "")
    {
        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFile)!, "../../7zFM2/Icons"));
    }
}
