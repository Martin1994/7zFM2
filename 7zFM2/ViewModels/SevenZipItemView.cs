namespace SevenZip.FileManager2.ViewModels;

public class SevenZipItemView : IItemView
{
    private SevenZipItemNode _node;
    public SevenZipItemNode Node => _node;

    public Symbol Icon => _node.Type == SevenZipItemType.File ? Symbol.Document : Symbol.Folder;

    public string Name => _node.Name;
    public string Size => _node.HasDetail ? _node.Detail.Size.ToString() : "";
    public string Modified => _node.HasDetail ? FormatDateTime(_node.Detail.ModifiedTime) : "";
    public string Created => _node.HasDetail ? FormatDateTime(_node.Detail.CreatedTime) : "";
    public string Comment => _node.HasDetail ? _node.Detail.Comment ?? "" : "";
    public string Folders => _node.Type == SevenZipItemType.Directory ? _node.Directories.ToString() : "";
    public string Files => _node.Type == SevenZipItemType.Directory ? _node.Files.ToString() : "";

    public SevenZipItemView(SevenZipItemNode node)
    {
        _node = node;
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
