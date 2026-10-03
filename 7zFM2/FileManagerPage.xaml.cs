using SevenZip.FileManager2.ViewModels;

namespace SevenZip.FileManager2;

public sealed partial class FileManagerPage : ContentPage
{
    private readonly FileManagerViewModel _vm;

    public FileManagerPage()
    {
        InitializeComponent();

        _vm = App.Services.GetRequiredService<FileManagerViewModel>();
        BindingContext = _vm;

        // TODO: the extract dialog is not ported yet - MAUI has no content-dialog primitive, and it
        // should come back as a separate window rather than an in-page overlay, so _vm.ExtractStarted
        // currently has no subscriber.
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _vm.SelectedItem = e.CurrentSelection.Count > 0 ? e.CurrentSelection[0] as IItemViewModel : null;
        _vm.SelectedItems = e.CurrentSelection.Cast<IItemViewModel>().ToArray();
    }

    private void OnItemDoubleTapped(object? sender, TappedEventArgs e)
    {
        _vm.Open();
    }
}
