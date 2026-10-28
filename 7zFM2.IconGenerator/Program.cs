using System.CommandLine;
using System.Runtime.CompilerServices;

namespace SevenZip.FileManager2.IconGenerator;

internal static class Program
{
    // No subcommand: generate both. icon1/icon2: generate only the selected icon.
    // --check validates without writing. Building alone never generates assets.
    private static int Main(string[] args)
    {
        var check = new Option<bool>("--check")
        {
            Description = "Check that the selected SVG files match their design source without writing them.",
            Recursive = true
        };
        var command = new RootCommand("Generate application icon SVGs. Defaults to all icons.") { check };
        var icon1 = new Command("icon1", "Generate the seven with a zipper tail.");
        var icon2 = new Command("icon2", "Generate the seven beside a vertical zipper.");
        command.Subcommands.Add(icon1);
        command.Subcommands.Add(icon2);
        command.SetAction(result => Generate(["icon1", "icon2"], result.GetValue(check)));
        icon1.SetAction(result => Generate(["icon1"], result.GetValue(check)));
        icon2.SetAction(result => Generate(["icon2"], result.GetValue(check)));
        return command.Parse(args).Invoke();
    }

    private static int Generate(string[] selection, bool check)
    {
        try
        {
            // Fully render the selection before writing anything, so invalid geometry does not
            // leave a partially regenerated batch.
            var images = selection.Select(name => name switch
            {
                "icon1" => (Name: Icon1.FileName, Content: Icon1.Render()),
                "icon2" => (Name: Icon2.FileName, Content: Icon2.Render()),
                _ => throw new ArgumentException("Unknown icon.")
            }).ToArray();

            string directory = GetOutputDirectory();
            foreach (var image in images)
            {
                string path = Path.Combine(directory, image.Name);
                if (check)
                {
                    if (!File.Exists(path) || File.ReadAllText(path) != image.Content)
                    {
                        throw new InvalidOperationException($"{image.Name} is missing or stale; regenerate it without --check.");
                    }
                    Console.WriteLine($"Up to date: {image.Name}");
                }
                else
                {
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(path, image.Content);
                    Console.WriteLine($"Generated: {path}");
                }
            }
            Console.WriteLine($"Preview: {new Uri(Path.Combine(directory, "preview.html")).AbsoluteUri}");
            return 0;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    // Source-relative location deliberately avoids depending on the shell's working directory.
    private static string GetOutputDirectory([CallerFilePath] string sourceFile = "")
    {
        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFile)!, "../7zFM2/Icons/AppIcons"));
    }
}