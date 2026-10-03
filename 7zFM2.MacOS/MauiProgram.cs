using Microsoft.Maui.Platforms.MacOS.Essentials;
using Microsoft.Maui.Platforms.MacOS.Hosting;

namespace SevenZip.FileManager2.MacOS;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiAppMacOS<App>()
            .AddMacOSEssentials();

        App.ConfigureServices(builder.Services);

        return builder.Build();
    }
}
