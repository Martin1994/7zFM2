namespace SevenZip.FileManager2.ViewModels;

public interface IItemViewModel
{
    // TODO: a UI-framework symbol enum is the wrong shape here, and nothing renders icons yet, so
    // the real type is still open (glyph string? icon-font alias? ImageSource?) - revisit with the
    // icon toolbar and the file-type-icon provider.
    string Icon { get; }
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
