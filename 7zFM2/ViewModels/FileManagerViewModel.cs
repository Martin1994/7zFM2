using System.Runtime.InteropServices;

namespace SevenZip.FileManager2.ViewModels;

public partial class FileManagerViewModel : ObservableObject
{
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

    [ObservableProperty]
    private string _name = "";

    public delegate void ExtractEventHandler(ArchiveExtractViewModel vm);
    public event ExtractEventHandler? ExtractStarted;

    partial void OnItemsChanging(IItemViewModel[] value)
    {
        Array.Sort(value, SortItems);
    }

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    public static extern int StrCmpLogicalW(string lhs, string rhs); // Should only be used on Windows

    private int SortItems(IItemViewModel lhs, IItemViewModel rhs)
    {
        if (lhs.IsDirectory == rhs.IsDirectory)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return StrCmpLogicalW(lhs.Name, rhs.Name);
            }
            else
            {
                return NaturalStringComparer.CurrentCulture.Compare(lhs.Name, rhs.Name);
            }
        }

        if (lhs.IsDirectory)
        {
            return -1;
        }
        else
        {
            return 1;
        }
    }

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
