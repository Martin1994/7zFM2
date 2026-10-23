using CommunityToolkit.Maui;
using SevenZip.FileManager2.Platform;

namespace SevenZip.FileManager2.Windows;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureMauiHandlers(handlers =>
            {
                handlers.AddHandler<Controls.StatusBar, WindowsStatusBarHandler>();
                handlers.AddHandler<Microsoft.Maui.Controls.Toolbar, WindowsToolbarHandler>();
            });

        App.ConfigureServices(builder.Services);
        builder.Services.AddSingleton<IDialogPresenter, WindowsDialogPresenter>();
        builder.Services.AddSingleton<IFileIconProvider, WindowsFileIconProvider>();
        builder.Services.AddSingleton<IPlatformWorkarounds, WindowsPlatformWorkarounds>();

        // TODO: Mica backdrop. MAUI has no cross-platform API for window composition, so this needs
        // a Windows-side platform hook - a WindowHandler customization over MauiWinUIWindow.

        return builder.Build();
    }
}
