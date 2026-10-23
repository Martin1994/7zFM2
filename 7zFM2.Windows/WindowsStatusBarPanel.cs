using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using ILayout = Microsoft.Maui.ILayout;

namespace SevenZip.FileManager2.Windows;

/// <summary>
/// Native status-row surface. A separate theme-brush property keeps the ThemeResource expression
/// intact when MAUI updates the panel's background or moves it to a non-hit-testable layer.
/// </summary>
public sealed class WindowsStatusBarPanel : LayoutPanel
{
    public static readonly DependencyProperty BackdropBrushProperty = DependencyProperty.Register(
        nameof(BackdropBrush), typeof(Microsoft.UI.Xaml.Media.Brush), typeof(WindowsStatusBarPanel),
        new PropertyMetadata(null, static (sender, _) =>
        {
            ((WindowsStatusBarPanel)sender).UpdateStatusBarBackground();
        }));

    public Microsoft.UI.Xaml.Media.Brush? BackdropBrush
    {
        get => (Microsoft.UI.Xaml.Media.Brush?)GetValue(BackdropBrushProperty);
        set => SetValue(BackdropBrushProperty, value);
    }

    internal void UpdateStatusBarBackground()
    {
        if (CrossPlatformLayout is ILayout layout)
        {
            // Respect an explicit MAUI background. Otherwise WinUI selects the material for
            // the element's current theme, including theme changes while the window is open.
            UpdateInputTransparent(layout.InputTransparent, layout.Background?.ToPlatform() ?? BackdropBrush);
        }
    }
}