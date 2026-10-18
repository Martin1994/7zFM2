// MAUI's implicit usings bring in System.Collections.Generic but not System.Collections, so a bare
// "IEnumerable" would resolve to the generic type.
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Maui;

namespace SevenZip.FileManager2.Controls;

/// <summary>
/// Displays a collection of items in a virtualized table with column headers and resizable columns.
/// </summary>
/// <remarks>
/// <para>
/// A general-purpose MAUI table control, conceptually similar to the WinUI CommunityToolkit Labs
/// DataTable, without a dependency on WinUI or application-specific data types. Each item in
/// <see cref="ItemsSource"/> represents a row. <see cref="Columns"/> defines the headers, widths,
/// and cell presentation; <see cref="DataTableColumn.CellTemplate"/> supports custom cell content.
/// </para>
/// <para>
/// Uses <see cref="CollectionView"/>, MAUI's recommended replacement for the deprecated ListView,
/// to present and virtualize rows. Each realized list item contains the cells for one row, with
/// widths shared by the header and all rows. Virtualization applies to rows, not individual columns;
/// its behavior is provided by the platform's CollectionView handler.
/// </para>
/// <para>
/// The header remains fixed during vertical scrolling. A shared horizontal scroll container keeps
/// headers and cells aligned when the columns exceed the viewport width. Place the control in a
/// container with a bounded height so the row presenter retains a finite virtualization viewport.
/// </para>
/// </remarks>
public partial class DataTable : ContentView
{
    private readonly CollectionView _list = new();
    private readonly Grid _surface;
    private readonly ScrollView _horizontalScroll;
    private readonly DataTableRow _header;
    private readonly List<WeakReference<DataTableRow>> _rows = [];
    private readonly HashSet<DataTableColumn> _observedColumns = [];
    private bool _updatingGeometry;
    private bool _viewportRefreshQueued;
    private IDisposable? _viewportObserver;
    private double _viewportWidth;

    internal double HorizontalOffset { get; private set; }

    public ObservableCollection<DataTableColumn> Columns { get; } = [];

    public DataTable()
    {
        // Apply the default before any items exist. The platform re-creates and re-measures item
        // views when the selection mode changes, and it attaches the row's selection gesture to
        // each item view as it is created - so the mode has to be right before the first reload.
        _list.SelectionMode = SelectionMode;
        _list.SelectionChanged += OnListSelectionChanged;
        _list.ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical) { ItemSpacing = 0 };
        _list.ItemSizingStrategy = ItemSizingStrategy.MeasureFirstItem;
        _list.HorizontalScrollBarVisibility = ScrollBarVisibility.Never;
        _list.ItemTemplate = new DataTemplate(CreateRow);

        _header = new DataTableRow(this, isHeader: true);
        _header.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#F0F0F0"), Color.FromArgb("#303030"));
        _surface = new Grid
        {
            RowSpacing = 0,
            ColumnSpacing = 0,
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) },
            ColumnDefinitions = { new ColumnDefinition(new GridLength(0)), new ColumnDefinition(GridLength.Star) },
        };
        Grid.SetColumnSpan(_header, 2);
        _surface.Children.Add(_header);
        Grid.SetRow(_list, 1);
        _surface.Children.Add(_list);
        _horizontalScroll = new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Default,
            VerticalScrollBarVisibility = ScrollBarVisibility.Never,
            Content = _surface,
        };
        Content = _horizontalScroll;
        Columns.CollectionChanged += OnColumnsChanged;
        _horizontalScroll.SizeChanged += OnViewportSizeChanged;
        _list.SizeChanged += OnViewportSizeChanged;
        _horizontalScroll.Scrolled += OnHorizontalScrolled;
        _horizontalScroll.HandlerChanged += OnScrollHandlerChanged;
        _list.HandlerChanged += OnScrollHandlerChanged;
        UpdateGeometry();
    }

    private IDataTablePlatformLayout? PlatformLayout =>
        Handler?.MauiContext?.Services.GetService<IDataTablePlatformLayout>();

    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        _viewportObserver?.Dispose();
        _viewportObserver = null;
        base.OnHandlerChanging(args);
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        OnScrollHandlerChanged(this, EventArgs.Empty);
    }

    private void OnScrollHandlerChanged(object? sender, EventArgs args)
    {
        _viewportObserver?.Dispose();
        _viewportObserver = null;
        if (_horizontalScroll.Handler is not null && _list.Handler is not null)
        {
            _viewportObserver = PlatformLayout?.ObserveViewport(_horizontalScroll, _list, QueueViewportRefresh);
        }

        QueueViewportRefresh();
    }

    private void QueueViewportRefresh()
    {
        if (_viewportRefreshQueued || Handler is null)
        {
            return;
        }

        _viewportRefreshQueued = true;
        // The GTK dispatcher can execute Dispatch inline; defer native allocation notifications.
        Dispatcher.DispatchDelayed(TimeSpan.Zero, () =>
        {
            _viewportRefreshQueued = false;
            if (Handler is not null)
            {
                UpdateGeometry();
            }
        });
    }

    // Uniform, fixed heights keep the virtualizing handler's item-size cache valid. Runtime height
    // changes are not supported by the pinned AppKit handler without reloading the entire list.
    public double RowHeight { get; } = 28d;

    public double HeaderHeight { get; } = 32d;

    private object CreateRow()
    {
        var row = new DataTableRow(this, isHeader: false);
        _rows.RemoveAll(reference =>
        {
            return !reference.TryGetTarget(out _);
        });
        _rows.Add(new WeakReference<DataTableRow>(row));
        return row;
    }

    private void OnColumnsChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        // Reconcile rather than only looking at OldItems: Clear() raises Reset without OldItems.
        foreach (var column in _observedColumns)
        {
            column.PropertyChanged -= OnColumnPropertyChanged;
        }

        _observedColumns.Clear();
        foreach (var column in Columns)
        {
            if (_observedColumns.Add(column))
            {
                column.PropertyChanged += OnColumnPropertyChanged;
            }
        }

        RebuildCells();
    }

    private void OnColumnPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(DataTableColumn.Width) or nameof(DataTableColumn.MinWidth)
            or nameof(DataTableColumn.MaxWidth))
        {
            UpdateGeometry();
        }
        else
        {
            RebuildCells();
        }
    }

    private void RebuildCells()
    {
        _header.RebuildCells();
        VisitRows(row =>
        {
            row.RebuildCells();
        });
        UpdateGeometry();
    }

    private void VisitRows(Action<DataTableRow> action)
    {
        for (var i = _rows.Count - 1; i >= 0; i--)
        {
            if (_rows[i].TryGetTarget(out var row))
            {
                action(row);
            }
            else
            {
                _rows.RemoveAt(i);
            }
        }
    }

    private void OnViewportSizeChanged(object? sender, EventArgs args)
    {
        UpdateGeometry();
    }

    private void OnHorizontalScrolled(object? sender, ScrolledEventArgs args)
    {
        if (!_updatingGeometry)
        {
            UpdateHorizontalOffset(refreshRows: true);
        }
    }

    private void UpdateHorizontalOffset(bool refreshRows)
    {
        // Legacy whole-row templates keep their original, document-wide scrolling contract.
        var offset = ItemTemplate is null
            ? Math.Clamp(_horizontalScroll.ScrollX, 0, Math.Max(0, _surface.WidthRequest - _viewportWidth))
            : 0;
        var changed = offset != HorizontalOffset;
        HorizontalOffset = offset;
        if (PlatformLayout is { } platform)
        {
            platform.SetHorizontalScrollOffset(_list, offset);
        }
        else
        {
            _list.TranslationX = offset;
        }

        if (changed && refreshRows)
        {
            VisitRows(row => row.RefreshHorizontalPosition());
        }
    }

    private void UpdateGeometry()
    {
        if (_updatingGeometry)
        {
            return;
        }

        _updatingGeometry = true;
        try
        {
            _header.IsVisible = Columns.Count > 0;
            var columnsWidth = Columns.Sum(column =>
            {
                return column.ActualWidth;
            });
            var platform = PlatformLayout;
            var viewport = platform?.GetViewportSize(_horizontalScroll)
                ?? new Size(_horizontalScroll.Width, _horizontalScroll.Height);
            _viewportWidth = Math.Max(0, viewport.Width);
            var rowViewportWidth = Math.Max(0, platform?.GetRowViewportWidth(_list) ?? _list.Width);
            var gutter = Math.Clamp(_list.Width - rowViewportWidth, 0, _viewportWidth);
            // The document owns column width, but the list (and its vertical scrollbar) owns only
            // the visible viewport. Include the scrollbar gutter so the final column is reachable.
            _surface.WidthRequest = Math.Max(columnsWidth + gutter, _viewportWidth);
            _surface.HeightRequest = Math.Max(0, viewport.Height);
            _list.HorizontalOptions = LayoutOptions.Fill;
            // Let the Grid own the width, not WidthRequest: the pinned GTK list measures from
            // its old allocation. An explicit view width makes MAUI retain/center that stale
            // DesiredSize when growing; unconstrained Fill uses the full Grid slot instead.
            _list.WidthRequest = -1;
            _surface.ColumnDefinitions[0].Width = new GridLength(
                ItemTemplate is null ? _viewportWidth : _surface.WidthRequest);

            if (_horizontalScroll.Handler is not null && _horizontalScroll.Width > 0 && _horizontalScroll.Height > 0)
            {
                // Updating WidthRequest alone does not schedule AppKit layout. Arrange at the
                // existing viewport to refresh the horizontal document extent, not the item source.
                _horizontalScroll.Handler.PlatformArrange(_horizontalScroll.Frame);
                var surface = (Microsoft.Maui.ILayout)_surface;
                surface.CrossPlatformMeasure(_surface.WidthRequest, _surface.HeightRequest);
                surface.CrossPlatformArrange(new Rect(0, 0, _surface.WidthRequest, _surface.HeightRequest));
            }

            UpdateHorizontalOffset(refreshRows: false);
            _header.RefreshGeometry();
            rowViewportWidth = Math.Max(0, platform?.GetRowViewportWidth(_list) ?? _list.Width);
            VisitRows(row =>
            {
                if (rowViewportWidth > 0)
                {
                    platform?.RefreshRow(row, rowViewportWidth);
                }

                row.RefreshGeometry();
            });

            var maxOffset = Math.Max(0, _surface.WidthRequest - _viewportWidth);
            if (_horizontalScroll.ScrollX > maxOffset)
            {
                _ = _horizontalScroll.ScrollToAsync(maxOffset, 0, animated: false);
            }
        }
        finally
        {
            _updatingGeometry = false;
        }
    }

    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

    /// <summary>
    /// Raised when a row is activated - two taps on the same item inside
    /// <see cref="ActivationInterval"/>. Opening an archive or entering a directory hangs off this.
    /// </summary>
    public event EventHandler? ItemActivated;

    /// <summary>
    /// How far apart two taps may be and still count as one activation.
    /// </summary>
    /// <remarks>
    /// TODO: ask the platform (NSEvent.DoubleClickInterval / GetDoubleClickTime) once a backend
    /// exposes it; MAUI has no cross-platform API for this.
    /// </remarks>
    public static readonly TimeSpan ActivationInterval = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// The items to render.
    /// </summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnItemsSourceChanged))]
    public partial IEnumerable? ItemsSource { get; set; }

    /// <summary>
    /// Optional legacy row template. Leave unset to generate cells from Columns.
    /// </summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnItemTemplateChanged))]
    public partial DataTemplate? ItemTemplate { get; set; }

    /// <summary>
    /// Single or multiple selection.
    /// </summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnSelectionModeChanged))]
    public partial SelectionMode SelectionMode { get; set; } = SelectionMode.Single;

    /// <summary>
    /// The selected row.
    /// </summary>
    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay, PropertyChangedMethodName = nameof(OnSelectedItemChanged))]
    public partial object? SelectedItem { get; set; }

    // The property-changed hooks have to be static and take (BindableObject, object, object) - that is
    // the signature the generated BindableProperty.Create call needs.
    private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var table = (DataTable)bindable;
        table._inputVersion++;
        table._lastTappedItem = null;
        table._lastTapTicks = 0;
        table._list.SelectedItem = null;
        table._list.SelectedItems.Clear();
        table.SelectedItem = null;
        table._list.ItemsSource = (IEnumerable?)newValue;
    }

    private static void OnItemTemplateChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var table = (DataTable)bindable;
        table._list.ItemTemplate = (DataTemplate?)newValue ?? new DataTemplate(table.CreateRow);
        table.UpdateGeometry();
    }

    private static void OnSelectionModeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((DataTable)bindable)._list.SelectionMode = (SelectionMode)newValue;
    }

    private static void OnSelectedItemChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var table = (DataTable)bindable;

        // Guards the round-trip that OnListSelectionChanged starts.
        if (table.SelectionMode == SelectionMode.Single && !ReferenceEquals(table._list.SelectedItem, newValue))
        {
            table._list.SelectedItem = newValue;
        }
    }

    private object? _lastTappedItem;
    private long _lastTapTicks;
    private long _inputVersion;

    internal Action<object?> CreateTapReporter()
    {
        var version = _inputVersion;
        return item =>
        {
            // Native selection can replace the row before an already-completed click is
            // delivered. Keep that click, but never replay it after navigation changes the data.
            if (version == _inputVersion)
            {
                ReportTap(item);
            }
        };
    }

    /// <summary>
    /// Records a tap on <paramref name="item"/>, reported by the row template's MAUI gesture
    /// or native input adapter, and raises <see cref="ItemActivated"/> when the tap completes a
    /// double tap.
    /// </summary>
    /// <remarks>
    /// The row template must use a <b>single</b>-tap recognizer.
    /// <c>TapGestureRecognizer NumberOfTapsRequired="2"</c> would stay in the "maybe" state after
    /// the first click, and on the MAUI Labs AppKit backend that keeps the row's own
    /// <c>NSClickGestureRecognizer</c> from ever recognising - the rows stop being selectable
    /// altogether. Pairing the taps here instead keeps the two gestures independent.
    /// </remarks>
    public void ReportTap(object? item)
    {
        if (item is null)
        {
            return;
        }

        var now = Environment.TickCount64;
        var isActivation = ReferenceEquals(item, _lastTappedItem)
            && now - _lastTapTicks <= (long)ActivationInterval.TotalMilliseconds;

        _lastTappedItem = isActivation ? null : item;
        _lastTapTicks = now;

        if (isActivation)
        {
            // The native selection recognizer may run after ours. Opening must target the row
            // that was activated, not the previously selected item.
            if (SelectionMode == SelectionMode.Single)
            {
                SelectedItem = item;
            }

            ItemActivated?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnListSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        SelectedItem = e.CurrentSelection.Count > 0 ? e.CurrentSelection[0] : null;
        SelectionChanged?.Invoke(this, e);
    }
}
