using SevenZip.FileManager2.Formats;

namespace SevenZip.FileManager2.ViewModels;

public class SevenZipItemViewModel : IItemViewModel
{
    private readonly SevenZipItemNode _node;
    public SevenZipItemNode Node => _node;

    public SevenZipInArchive Archive => _node.Archive;

    public Symbol Icon => _node.Type == SevenZipItemType.File ? Symbol.Document : Symbol.Folder;

    public string Name => _node.Name;
    public string Size => _node.HasDetail ? SizeFormatter.FromBytes(_node.Detail.Size) : "";
    public string Modified => _node.HasDetail ? FormatDateTime(_node.Detail.ModifiedTime) : "";
    public string Created => _node.HasDetail ? FormatDateTime(_node.Detail.CreatedTime) : "";
    public string Comment => _node.HasDetail ? _node.Detail.Comment ?? "" : "";
    public string Folders => _node.Type == SevenZipItemType.Directory ? _node.Directories.ToString("N0") : "";
    public string Files => _node.Type == SevenZipItemType.Directory ? _node.Files.ToString("N0") : "";

    public SevenZipItemViewModel(SevenZipItemNode node)
    {
        _node = node;
    }

    public void Open(FileManagerViewModel fm)
    {
        if (_node.Type == SevenZipItemType.Directory)
        {
            IItemViewModel[] children = new IItemViewModel[_node.ChildrenCount];
            int i = 0;
            foreach (var child in _node.Children)
            {
                children[i++] = new SevenZipItemViewModel(child);
            }
            fm.Items = children;
            fm.TotalItems = children.Length;
            fm.ReturnAction = () => new SevenZipItemViewModel(_node.Parent).Open(fm);
        }
    }

    private static string FormatDateTime(DateTime date)
    {
        if (date == default)
        {
            return "";
        }
        return date.ToString();
    }
}
