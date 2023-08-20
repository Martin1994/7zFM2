namespace SevenZip.FileManager2.ViewModels;

public interface IItemViewModel
{
    Symbol Icon { get; }
    string Name { get; }
    bool IsDirectory { get; }
    string Size { get; }
    string Modified { get; }
    string Created { get; }
    string Comment { get; }
    string Folders { get; }
    string Files { get; }

    void Open(FileManagerViewModel fm);
}
