using SevenZip.FileManager2.Formats;

namespace SevenZip.FileManager2.ViewModels;

public class SystemDirectoryViewModel : IItemViewModel
{
    private readonly DirectoryInfo _info;

    public SystemDirectoryViewModel(DirectoryInfo info)
    {
        _info = info;
    }

    public Symbol Icon => Symbol.Folder;

    public string Name => _info.Name;

    public string Size => "";

    public string Modified => _info.LastWriteTime.ToString();

    public string Created => _info.CreationTime.ToString();

    public string Comment => "";

    public string Folders => "";

    public string Files => "";

    public void Open(FileManagerViewModel fm)
    {
        var parent = _info.Parent;
        fm.ReturnAction = parent == null ?
            () => new SystemDriveViewModel().Open(fm) :
            () => new SystemDirectoryViewModel(parent).Open(fm);
        fm.Name = PathFormatter.AddTrailingSlash(_info.FullName);

        var children = _info.GetFileSystemInfos().Select(info =>
        {
            if (info is DirectoryInfo)
            {
                return new SystemDirectoryViewModel((DirectoryInfo)info);
            }

            return (IItemViewModel)new SystemFileViewModel((FileInfo)info);
        }).ToArray();

        fm.Items = children;
    }

}
