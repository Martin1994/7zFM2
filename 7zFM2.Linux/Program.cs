using Microsoft.Maui.Platforms.Linux.Gtk4.Platform;

namespace SevenZip.FileManager2.Linux;

public class Program : GtkMauiApplication
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public static void Main(string[] args)
    {
        var app = new Program();
        app.Run(args);
    }
}
