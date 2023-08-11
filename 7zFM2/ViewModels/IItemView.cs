namespace SevenZip.FileManager2.ViewModels;

public interface IItemView
{
    Symbol Icon { get; }
    string Name { get; }
    string Size { get; }
    string Modified { get; }
    string Created { get; }
    string Comment { get; }
    string Folders { get; }
    string Files { get; }
}
