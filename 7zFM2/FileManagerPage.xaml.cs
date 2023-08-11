using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Input;
using SevenZip.FileManager2.Controls;
using SevenZip.FileManager2.ViewModels;
using System.Runtime.InteropServices;

namespace SevenZip.FileManager2;

public sealed partial class FileManagerPage : Page
{
    private IItemView[] Items
    {
        set
        {
            if (!DispatcherQueue.HasThreadAccess)
            {
                var task = DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    Items = value;
                });
                return;
            }

            _items = value;
            _totalItems = value.Length;
            ItemDataGrid.ItemsSource = value;
            UpdateSelectionStatus();
        }
    }

    private IItemView[] _items = new IItemView[0];
    private int _totalItems;
    private SevenZipItemNode? _currentNode;

    public FileManagerPage()
    {
        InitializeComponent();
        Loaded += (sender, e) => Task.Run(TestDataGrid);

        ItemDataGrid.SelectionChanged += (sender, e) => UpdateSelectionStatus();
    }

    private void TestDataGrid()
    {
        var path = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
            @"E:\Backups\Music.7z" :
            @"/mnt/e/Backups/Music.7z";
        var stream = new FileStream(
            path,
            FileMode.Open, FileAccess.Read, FileShare.Read
        );
        var arc = new SevenZipInArchive(path, stream);
        DispatcherQueue.TryEnqueue(() =>
        {
            (Application.Current as App)!.MainWindow!.Title = $"{path} - 7-zip File Manager 2";
        });
        ListNode(arc.ItemTree);
    }

    private void ListNode(SevenZipItemNode node)
    {
        _currentNode = node;
        IItemView[] items = new IItemView[node.ChildrenCount];
        int i = 0;
        foreach (var child in node.Children)
        {
            items[i++] = new SevenZipItemView(child);
        }
        Items = items;
    }

    private void UpdateSelectionStatus()
    {
        int selected = ItemDataGrid.SelectedItems.Count;
        SelectionStatus.Text = $"{selected} / {_totalItems} object{(selected == 1 ? "" : "s")} selected";
    }

    private void OnReturn(object sender, RoutedEventArgs e)
    {
        SevenZipItemNode currentNode = (SevenZipItemNode)_currentNode!;
        SevenZipItemNode parentNode = currentNode.Parent;

        ListNode(parentNode);
    }

    private void OnItemGridDoubleClick(object sender, DoubleTappedRoutedEventArgs e)
    {
        DataGridRow? pressedRow;

        // Determine the pressed row
        try
        {
            DependencyObject? source = e.OriginalSource as DependencyObject;
            if (source == null)
            {
                return;
            }
            pressedRow = FindParent<DataGridRow>(source);
        }
        catch
        {
            return;
        }

        if (pressedRow == null)
        {
            return;
        }

        SevenZipItemNode selectedNode = ((SevenZipItemView)_items[pressedRow.GetIndex()]).Node;

        if (selectedNode.Type == SevenZipItemType.File)
        {
            return;
        }

        ListNode(selectedNode);
    }

    private async void OnTest(object sender, RoutedEventArgs e)
    {
        SevenZipItemNode currentNode = (SevenZipItemNode)_currentNode!;
        var selectedNodes = ItemDataGrid.SelectedItems.Count == 0 ?
            new SevenZipItemNode[] { currentNode } :
            ItemDataGrid.SelectedItems.Cast<IItemView>().Select(view => currentNode[view.Name]);

        var extract = new ExtractDialog()
        {
            ArchiveExtract = new ArchiveExtract(),
            XamlRoot = XamlRoot
        };

        extract.ArchiveExtract.Extract(currentNode.Archive, selectedNodes);
        await extract.ShowAsync();
    }

    private void OnExtract(object sender, RoutedEventArgs e)
    {
        SevenZipItemNode currentNode = (SevenZipItemNode)_currentNode!;
        var selectedNodes = ItemDataGrid.SelectedItems.Cast<IItemView>().SelectMany(view => currentNode[view.Name].Traverse());

        //currentNode.Archive.Extract(selectedNodes, NAskMode.kExtract, null!);
    }

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        T? parent = default(T);
        DependencyObject currParent;

        currParent = VisualTreeHelper.GetParent(child);
        while (currParent != null)
        {
            if (currParent is T)
            {
                parent = (T)currParent;
                break;
            }

            currParent = VisualTreeHelper.GetParent(currParent);
        }

        return parent;
    }
}
