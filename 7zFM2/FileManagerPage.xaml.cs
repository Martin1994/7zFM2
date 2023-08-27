using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
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
        ListView list = (sender as ListView)!;
        _vm.SelectedItem = list.SelectedItem as IItemViewModel;
        _vm.SelectedItems = new CastedList<IItemViewModel>(list.SelectedItems);
    }

    private async void OnExtract(ArchiveExtractViewModel vm)
    {
        await new ArchiveExtractDialog()
        {
            ArchiveExtract = vm,
            XamlRoot = XamlRoot
        }.ShowAsync();
    }

    private void OnDoubleTapItem(object sender, DoubleTappedRoutedEventArgs args)
    {
        if (!(args.OriginalSource is DependencyObject))
        {
            return;
        }

        var row = FindParent<ListViewItemPresenter>((args.OriginalSource as DependencyObject)!);
        if (row == null)
        {
            return;
        }

        _vm.Open();
    }

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        DependencyObject current = child;

        do
        {
            if (current is T)
            {
                return (T)current;
            }

            current = VisualTreeHelper.GetParent(current);
        } while (current != null);

        return default;
    }
}

file class CastedList<T> : IReadOnlyList<T>
{
    private readonly IList<object> _original;

    public T this[int index] => (T?)_original[index] ?? throw new NullReferenceException();

    public int Count => _original.Count;

    public CastedList(IList<object> original)
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
