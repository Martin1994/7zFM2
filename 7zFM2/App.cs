using SevenZip.FileManager2.ViewModels;
using System.Runtime.InteropServices;

namespace SevenZip.FileManager2;

public class App : Application
{
    public static new App Current => (Application.Current as App)!;
    public static Window MainWindow { get; private set; } = null!;
    public IHost Host { get; private set; } = null!;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            .Configure(host => host
#if DEBUG
            // Switch to Development environment when running in DEBUG
            .UseEnvironment(Environments.Development)
#endif
            .UseLogging(configure: (context, logBuilder) =>
            {
                // Configure log levels for different categories of logging
                logBuilder
                    .SetMinimumLevel(
                        context.HostingEnvironment.IsDevelopment() ?
                            LogLevel.Information :
                            LogLevel.Warning)

                    // Default filters for core Uno Platform namespaces
                    .CoreLogLevel(LogLevel.Warning);

                // Uno Platform namespace filter groups
                // Uncomment individual methods to see more detailed logging
                //// Generic Xaml events
                //logBuilder.XamlLogLevel(LogLevel.Debug);
                //// Layouter specific messages
                //logBuilder.XamlLayoutLogLevel(LogLevel.Debug);
                //// Storage messages
                //logBuilder.StorageLogLevel(LogLevel.Debug);
                //// Binding related messages
                //logBuilder.XamlBindingLogLevel(LogLevel.Debug);
                //// Binder memory references tracking
                //logBuilder.BinderMemoryReferenceLogLevel(LogLevel.Debug);
                //// RemoteControl and HotReload related
                //logBuilder.HotReloadCoreLogLevel(LogLevel.Information);
                //// Debug JS interop
                //logBuilder.WebAssemblyLogLevel(LogLevel.Debug);

            }, enableUnoLogging: true)

            .ConfigureServices(ConfigureServices)
        );
        MainWindow = builder.Window;

        Host = builder.Build();

        // Do not repeat app initialization when the Window already has content,
        // just ensure that the window is active
        if (MainWindow.Content is not Frame rootFrame)
        {
            // Create a Frame to act as the navigation context and navigate to the first page
            rootFrame = new Frame();

            // Place the frame in the current Window
            MainWindow.Content = rootFrame;
        }

        if (rootFrame.Content == null)
        {
            // When the navigation stack isn't restored navigate to the first page,
            // configuring the new page by passing required information as a navigation
            // parameter
            rootFrame.Navigate(typeof(FileManagerPage), args.Arguments);
        }

        InitializeWindow();

        // Ensure the current window is active
        MainWindow.Activate();
    }

    protected virtual void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.AddScoped(ProvideItemTree);
    }

    protected virtual FileManagerViewModel ProvideItemTree(IServiceProvider provider)
    {
        var fm = new FileManagerViewModel();

        MainWindow.Title = "7-zip File Manager 2";

        new SystemDirectoryViewModel(new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))).Open(fm);

        return fm;
    }

    protected virtual void InitializeWindow()
    {
    }
}
