using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Media;

namespace SevenZip.FileManager2;

public sealed partial class AppHead : App
{
    protected override void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        base.ConfigureServices(context, services);
    }

    protected override void InitializeWindow()
    {
        base.InitializeWindow();

        MainWindow.SystemBackdrop = new MicaBackdrop()
        {
            Kind = MicaKind.Base
        };
    }
}
