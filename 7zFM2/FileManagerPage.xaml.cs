using Microsoft.UI.Xaml.Input;
using SevenZip.FileManager2.Controls;
using SevenZip.FileManager2.Models;
using SevenZip.FileManager2.ViewModels;
using System.Collections;
using Windows.Foundation;

namespace SevenZip.FileManager2;

public sealed partial class FileManagerPage : Page
{
    private readonly FileManagerViewModel _vm;
    private readonly MultiTapConfiguration _multiTapConfiguration;

    public FileManagerPage()
    {
        InitializeComponent();

        _vm = App.Current.Host.Services.GetRequiredService<FileManagerViewModel>();
        _multiTapConfiguration = App.Current.Host.Services.GetRequiredService<MultiTapConfiguration>();

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

#if HAS_UNO
    private struct Tap
    {
        public long Tick;
        public Point Position;
    }

    private Tap _lastTap = default;

    private Point GetTapPosition(TappedRoutedEventArgs args)
    {

#if __MACOS__
        // MacOS don't have implemented UIElement::GetPosition
        var originalSource = args.OriginalSource as UIElement;
        if (originalSource == null)
        {
            return default(Point);
        }
        var position = args.GetPosition(originalSource);
        if (this == originalSource)
        {
            return position;
        }
        return TransformToVisual(originalSource).TransformPoint(position);
#else
        return args.GetPosition(this);
#endif
    }

    private void OnTapItem(object sender, TappedRoutedEventArgs args)
    {
        // Uno does not implement DoubleTapped event properly in a nested view as of 2023/09/01,
        // so we have to simulate a DoubleTapped event by our own here.

        var position = GetTapPosition(args);
        var now = DateTime.Now.Ticks;

        if (
            _lastTap.Tick > now - _multiTapConfiguration.MultiTapMultiTapMaxDelayTicks &&
            Math.Abs(_lastTap.Position.X - position.X) < _multiTapConfiguration.TapMaxXDelta &&
            Math.Abs(_lastTap.Position.Y - position.Y) < _multiTapConfiguration.TapMaxYDelta
        )
        {
            args.Handled = true;
            _lastTap = default;
            OnDoubleTapItem(sender, args);
        }
        else
        {
            _lastTap = new()
            {
                Tick = now,
                Position = position
            };
        }

    }
#endif
    private void OnDoubleTapItem(object sender, RoutedEventArgs args)
    {
        if (!(args.OriginalSource is DependencyObject))
        {
            return;
        }

        var row = FindParent<ItemsPresenter>((args.OriginalSource as DependencyObject)!);
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
            if (current is T t)
            {
                return t;
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
