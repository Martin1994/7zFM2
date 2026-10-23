using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace SevenZip.FileManager2.Windows;

/// <summary>
/// Applies the application's compact desktop menu style to the MAUI toolbar on Windows.
/// This is a presentation choice, not a workaround for a backend defect.
/// </summary>
internal sealed class WindowsToolbarHandler : ToolbarHandler
{
    protected override void ConnectHandler(MauiToolbar platformView)
    {
        base.ConnectHandler(platformView);

        if (platformView.FindName("menuContent") is ContentControl menuHost)
        {
            // Preserve the native MenuBar's 40-DIP minimum and normal item margins instead
            // of stretching its buttons into the taller navigation toolbar slot.
            menuHost.VerticalContentAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center;
            platformView.Resources["AppBarThemeCompactHeight"] = 40d;
            platformView.MinHeight = 0;
            platformView.Loaded += OnToolbarLoaded;
        }
    }

    protected override void DisconnectHandler(MauiToolbar platformView)
    {
        platformView.Loaded -= OnToolbarLoaded;
        base.DisconnectHandler(platformView);
    }

    private static void OnToolbarLoaded(object sender, RoutedEventArgs args)
    {
        // The containing NavigationView header has a separate minimum height. Let this
        // toolbar's content determine its height, without affecting other headers.
        var parent = VisualTreeHelper.GetParent((DependencyObject)sender);
        while (parent is not null)
        {
            if (parent is ContentControl { Name: "HeaderContent" } header)
            {
                header.MinHeight = 0;
                return;
            }

            parent = VisualTreeHelper.GetParent(parent);
        }
    }
}