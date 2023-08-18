using SevenZip.FileManager2.Formats;

namespace SevenZip.FileManager2.ViewModels;

public class SystemFileViewModel : IItemViewModel
{
    private readonly FileInfo _info;

    public SystemFileViewModel(FileInfo info)
    {
        _info = info;
    }

    public Symbol Icon => Symbol.Document;

    public string Name => _info.Name;

    public string Size => SizeFormatter.FromBytes(_info.Length);

    public string Modified => _info.LastWriteTime.ToString();

    public string Created => _info.CreationTime.ToString();

    public string Comment => "";

    public string Folders => "";

    public string Files => "";

    public void Open(FileManagerViewModel fm)
    {
        if (SevenZipArchiveFormat.FromPath(Name) != null)
        {
            var stream = File.Open(_info.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
            var arc = new SevenZipInArchive(Name, stream);
            new SevenZipItemViewModel(arc.RootNode).Open(fm);
            fm.Name = PathFormatter.AddTrailingSlash(_info.FullName);
            return;
        }

        // TODO: xdg-open
    }

}
