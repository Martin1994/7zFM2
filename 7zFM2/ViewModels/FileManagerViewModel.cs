namespace SevenZip.FileManager2.ViewModels;

public partial class FileManagerViewModel : ObservableObject
{
    [ObservableProperty]
    private int _totalItems;

    [ObservableProperty]
    private IItemViewModel[] _items = Array.Empty<IItemViewModel>();

    [ObservableProperty]
    private Action _returnAction = () => { };

    [ObservableProperty]
    private string _selectionStatus = "";


    [ObservableProperty]
    private IItemViewModel? _selectedItem = null;

    [ObservableProperty]
    private IReadOnlyList<IItemViewModel> _selectedItems = Array.Empty<IItemViewModel>();

    public delegate void ExtractEventHandler(ArchiveExtractViewModel vm);
    public event ExtractEventHandler? ExtractStarted;

    partial void OnItemsChanged(IItemViewModel[] value)
    {
        UpdateSelectionStatus();
    }

    partial void OnSelectedItemsChanged(IReadOnlyList<IItemViewModel> value)
    {
        UpdateSelectionStatus();
    }

    private void UpdateSelectionStatus()
    {
        var selected = SelectedItems.Count;
        var total = Items.Length;
        SelectionStatus = $"{selected} / {total} object{(selected == 1 ? "" : "s")} selected";
    }

    [RelayCommand]
    public void Open()
    {
        if (SelectedItem != null)
        {
            SelectedItem.Open(this);
        }
    }

    [RelayCommand]
    public void Return()
    {
        ReturnAction();
    }

    [RelayCommand]
    public void Test()
    {
        if (SelectedItems.Count == 0) throw new InvalidOperationException("Must select at least one node");
        // TODO: get current node after tracking all parent nodes

        // TODO: test archive from fs
        var archive = SelectedItems.Cast<SevenZipItemViewModel>().First().Archive;
        var selectedNodes = SelectedItems.Cast<SevenZipItemViewModel>().Select(vm => vm.Node);

        var extract = new ArchiveExtractViewModel();
        extract.Extract(archive, selectedNodes);

        if (ExtractStarted != null)
        {
            ExtractStarted(extract);
        }
    }
}
