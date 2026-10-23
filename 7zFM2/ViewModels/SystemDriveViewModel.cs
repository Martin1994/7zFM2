using SevenZip.FileManager2.Formats;
using SevenZip.FileManager2.Icons;

namespace SevenZip.FileManager2.ViewModels;

public class SystemDriveViewModel : IItemViewModel
{

    public FileIconRequest Icon => new(FileIconKind.Computer, Name);

    public string Name => Environment.MachineName;

    public bool IsDirectory => true;

    public string Size => "";

    public string Modified => "";

    public string Created => "";

    public string Comment => "";

    public string Folders => "";

    public string Files => "";

    public void Open(FileManagerViewModel fm)
    {
        fm.ReturnAction = () => { };
        fm.Name = PathFormatter.AddTrailingSlash(Name);

        var children = DriveInfo.GetDrives().Select(info =>
        {
            return new SystemDirectoryViewModel(info.RootDirectory);
        }).ToArray();

        fm.Items = children;
    }

}
