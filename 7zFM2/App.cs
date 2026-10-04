using SevenZip.FileManager2.ViewModels;

namespace SevenZip.FileManager2;

public class App : Application
{
    public static new App Current => (Application.Current as App)!;

    /// <summary>
    /// The application's service provider.
    /// </summary>
    /// <remarks>
    /// The container is the one built in each head's <c>MauiProgram</c>; MAUI hands it back through
    /// <see cref="IPlatformApplication"/>.
    /// </remarks>
    public static IServiceProvider Services =>
        IPlatformApplication.Current?.Services
            ?? throw new InvalidOperationException("The MAUI service provider is not available yet.");

    public App()
    {
        Resources.MergedDictionaries.Add(new AppResources());
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Shell rather than the page directly: on Windows the ContentPage menu bar is hosted by the
        // window's toolbar, which only Shell/NavigationPage provide. See AppShell.
        return new Window(new AppShell())
        {
            Title = "7-zip File Manager 2",
        };
    }

    /// <summary>
    /// Registers the services shared by every platform head.
    /// </summary>
    /// <remarks>
    /// Called from each head's <c>MauiProgram.CreateMauiApp</c>. Anything platform-specific is
    /// registered by the head itself.
    /// </remarks>
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped(ProvideFileManager);
    }

    private static FileManagerViewModel ProvideFileManager(IServiceProvider provider)
    {
        var fm = new FileManagerViewModel();

        new SystemDirectoryViewModel(new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))).Open(fm);

        return fm;
    }
}
