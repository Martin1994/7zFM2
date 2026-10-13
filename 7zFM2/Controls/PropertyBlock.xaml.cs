using CommunityToolkit.Maui;

namespace SevenZip.FileManager2.Controls;

/// <summary>
/// A titled value, e.g. "Total Size" over "1.5 MiB".
/// </summary>
public partial class PropertyBlock : ContentView
{
    public PropertyBlock()
    {
        InitializeComponent();
    }

    [BindableProperty]
    public partial string? Title { get; set; }

    [BindableProperty]
    public partial string? Text { get; set; }
}
