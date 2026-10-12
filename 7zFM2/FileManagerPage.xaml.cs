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

        FileList.ItemActivated += OnItemActivated;

        // TODO: the extract dialog is not ported yet - MAUI has no content-dialog primitive, and it
        // should come back as a separate window rather than an in-page overlay, so _vm.ExtractStarted
        // currently has no subscriber.
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _vm.SelectedItem = e.CurrentSelection.Count > 0 ? e.CurrentSelection[0] as IItemViewModel : null;
        _vm.SelectedItems = e.CurrentSelection.Cast<IItemViewModel>().ToArray();
    }

    private void OnItemTapped(object? sender, TappedEventArgs e)
    {
        // The row template reports every tap; DataTable decides when two of them make an activation.
        if ((sender as VisualElement)?.BindingContext is IItemViewModel item)
        {
            FileList.ReportTap(item);
        }
    }

    private async void OnItemActivated(object? sender, EventArgs e)
    {
        try
        {
            _vm.Open();
        }
        catch (Exception ex)
        {
            // Expected failure on user data: a truncated archive, a format the library does not know,
            // a locked volume. Not a bug, so it must not go to the unhandled-exception hooks - those
            // report and let the process die. Telling the user is the entire recovery.

            // TODO: the platform's system alert, standing in for the app's own dialog.
            await DisplayAlertAsync("Cannot open", ex.Message, "OK");
        }
    }
}
