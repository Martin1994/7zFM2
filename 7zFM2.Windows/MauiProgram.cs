namespace SevenZip.FileManager2.Windows;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder.UseMauiApp<App>();

        App.ConfigureServices(builder.Services);

        // TODO: Mica backdrop. MAUI has no cross-platform API for window composition, so this needs
        // a Windows-side platform hook - a WindowHandler customization over MauiWinUIWindow.

        return builder.Build();
    }
}
