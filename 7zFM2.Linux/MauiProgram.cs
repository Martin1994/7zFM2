using Microsoft.Maui.Platforms.Linux.Gtk4.Essentials.Hosting;
using Microsoft.Maui.Platforms.Linux.Gtk4.Hosting;
using SevenZip.FileManager2.Platform;

namespace SevenZip.FileManager2.Linux;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiAppLinuxGtk4<App>()
            .AddLinuxGtk4Essentials();

        App.ConfigureServices(builder.Services);
        builder.Services.AddSingleton<IDialogPresenter, GtkDialogPresenter>();
        builder.Services.AddSingleton<IPlatformWorkarounds, GtkPlatformWorkarounds>();

        return builder.Build();
    }
}
