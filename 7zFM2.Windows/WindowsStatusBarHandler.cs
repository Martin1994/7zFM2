using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using ILayout = Microsoft.Maui.ILayout;

namespace SevenZip.FileManager2.Windows;

/// <summary>
/// Gives the shared status row a WinUI theme background without changing MAUI layout.
/// </summary>
internal sealed class WindowsStatusBarHandler : LayoutHandler
{
    private static readonly IPropertyMapper<ILayout, WindowsStatusBarHandler> StatusBarMapper =
        new PropertyMapper<ILayout, WindowsStatusBarHandler>(LayoutHandler.Mapper)
        {
            // LayoutPanel couples its background with hit testing; both mappings must use
            // the same brush so changing InputTransparent does not erase the native material.
            [nameof(ILayout.Background)] = MapStatusBarBackground,
            [nameof(ILayout.InputTransparent)] = MapStatusBarBackground,
        };

    public WindowsStatusBarHandler() : base(StatusBarMapper)
    {
    }

    protected override LayoutPanel CreatePlatformView()
    {
        return new WindowsStatusBarPanel
        {
            CrossPlatformLayout = VirtualView,
            Style = (Microsoft.UI.Xaml.Style)Microsoft.UI.Xaml.Application.Current.Resources["StatusBarPanelStyle"],
        };
    }

    private static void MapStatusBarBackground(WindowsStatusBarHandler handler, ILayout layout)
    {
        ((WindowsStatusBarPanel)handler.PlatformView).UpdateStatusBarBackground();
    }
}