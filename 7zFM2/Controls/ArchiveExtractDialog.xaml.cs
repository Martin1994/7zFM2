namespace SevenZip.FileManager2.Controls;

public sealed partial class ArchiveExtractDialog : ContentPage
{
    public ArchiveExtractDialog()
    {
        InitializeComponent();
    }

    // Dismissing is the page's own business; the presenter only shows it.
    private async void OnOk(object? sender, EventArgs e) => await Navigation.PopModalAsync();
}
