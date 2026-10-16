using SevenZip.FileManager2.Controls;
using SevenZip.FileManager2.Platform;
using SevenZip.FileManager2.ViewModels;

namespace SevenZip.FileManager2;

public sealed partial class FileManagerPage : ContentPage
{
    private readonly FileManagerViewModel _vm;
    private readonly IDialogPresenter _dialogs;
    private readonly IPlatformWorkarounds _workarounds;

    public FileManagerPage()
    {
        InitializeComponent();

        _vm = App.Services.GetRequiredService<FileManagerViewModel>();
        _dialogs = App.Services.GetRequiredService<IDialogPresenter>();
        _workarounds = App.Services.GetRequiredService<IPlatformWorkarounds>();
        BindingContext = _vm;

        FileList.ItemActivated += OnItemActivated;
        _vm.ExtractStarted += OnExtractStarted;

        _workarounds.OnPageCreated(this);
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _vm.SelectedItem = e.CurrentSelection.Count > 0 ? e.CurrentSelection[0] as IItemViewModel : null;
        _vm.SelectedItems = e.CurrentSelection.Cast<IItemViewModel>().ToArray();
    }

    // The Uno original used a ContentDialog. Each head presents this its own way - on macOS as a
    // sheet attached to the main window, which is also what keeps that window out of reach for as
    // long as the run lasts.
    private async void OnExtractStarted(ArchiveExtractViewModel extract)
    {
        var dialog = new ArchiveExtractDialog { BindingContext = extract };
        await _dialogs.ShowAsync(this, dialog);
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
