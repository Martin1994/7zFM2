using SevenZip.FileManager2.Controls;
using SevenZip.FileManager2.ViewModels;
using System.Collections;

namespace SevenZip.FileManager2;

public sealed partial class FileManagerPage : Page
{
    private readonly FileManagerViewModel _vm;

    public FileManagerPage()
    {
        InitializeComponent();

        _vm = App.Current.Host.Services.GetRequiredService<FileManagerViewModel>();

        _vm.ExtractStarted += OnExtract;
    }
    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        DataGrid d = (sender as DataGrid)!;
        _vm.SelectedItem = d.SelectedItem as IItemViewModel;
        _vm.SelectedItems = new CastedList<IItemViewModel>(d.SelectedItems);
    }

    private async void OnExtract(ArchiveExtractViewModel vm)
    {
        await new ArchiveExtractDialog()
        {
            ArchiveExtract = vm,
            XamlRoot = XamlRoot
        }.ShowAsync();
    }

    private void OnLoadingRow(object sender, DataGridRowEventArgs e)
    {
        e.Row.DoubleTapped += (sender, e) => _vm.Open();
    }
}

file class CastedList<T> : IReadOnlyList<T>
{
    private readonly IList _original;

    public T this[int index] => (T?)_original[index] ?? throw new NullReferenceException();

    public int Count => _original.Count;

    public CastedList(IList original)
    {
        _original = original;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return _original.Cast<T>().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _original.GetEnumerator();
    }
}
