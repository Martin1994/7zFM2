using Microsoft.Maui.Layouts;

namespace SevenZip.FileManager2.Controls;

/// <summary>
/// One CollectionView item, not a list of cells. The layout reads the same column objects as the
/// header; recycled/new rows therefore never depend on a cached copy of the widths.
/// </summary>
internal sealed class DataTableRow : Layout
{
    private readonly DataTable _table;
    private readonly bool _isHeader;
    private readonly TapGestureRecognizer? _tapRecognizer;
    private IDisposable? _platformInput;
    private Rect _arrangedBounds;

    internal DataTableRow(DataTable table, bool isHeader)
    {
        _table = table;
        _isHeader = isHeader;
        IsClippedToBounds = true;
        HeightRequest = isHeader ? table.HeaderHeight : table.RowHeight;
        MinimumHeightRequest = HeightRequest;

        if (!isHeader)
        {
            _tapRecognizer = new TapGestureRecognizer();
            _tapRecognizer.Tapped += OnTapped;
            GestureRecognizers.Add(_tapRecognizer);
        }

        RebuildCells();
    }

    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        _platformInput?.Dispose();
        _platformInput = null;
        base.OnHandlerChanging(args);
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        UpdateInput();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (Handler is not null)
        {
            UpdateInput();
        }
    }

    private void UpdateInput()
    {
        if (_tapRecognizer is null)
        {
            return;
        }

        _platformInput?.Dispose();
        _platformInput = Handler?.MauiContext?.Services.GetService<IDataTablePlatformInput>()
            ?.AttachRow(this, _table.CreateTapReporter());

        if (_platformInput is not null)
        {
            GestureRecognizers.Remove(_tapRecognizer);
        }
        else if (!GestureRecognizers.Contains(_tapRecognizer))
        {
            GestureRecognizers.Add(_tapRecognizer);
        }
    }

    internal void RebuildCells()
    {
        Children.Clear();
        foreach (var column in _table.Columns)
        {
            Children.Add(_isHeader ? CreateHeader(column) : CreateCell(column));
        }
    }

    private static View CreateCell(DataTableColumn column)
    {
        if (column.CellTemplate is not null)
        {
            return column.CellTemplate.CreateContent() as View
                ?? throw new InvalidOperationException("DataTableColumn.CellTemplate must create a View.");
        }

        var label = CreateLabel(column);
        label.SetBinding(Label.TextProperty, new Binding(column.BindingPath)
        {
            StringFormat = column.StringFormat,
        });
        return label;
    }

    private View CreateHeader(DataTableColumn column)
    {
        var cell = new Grid { IsClippedToBounds = true };
        var label = CreateLabel(column);
        label.Text = column.Header;
        label.FontAttributes = FontAttributes.Bold;
        cell.Children.Add(label);

        // A wide enough target to acquire with a mouse, rather than a one-pixel-only hit region.
        var handle = new BoxView
        {
            WidthRequest = 8,
            HorizontalOptions = LayoutOptions.End,
            Margin = new Thickness(0, 6),
        };
        // A single hit-testable surface: this AppKit backend does not honor InputTransparent on
        // a decorative child BoxView, which would otherwise swallow drags on the separator.
        handle.SetAppThemeColor(BoxView.ColorProperty, Color.FromArgb("#D0D0D0"), Color.FromArgb("#606060"));
        SemanticProperties.SetDescription(handle, $"Resize {column.Header} column");

        if (column.CanResize)
        {
            var startWidth = 0d;
            var dragging = false;
            var pan = new PanGestureRecognizer();
            pan.PanUpdated += (_, args) =>
            {
                switch (args.StatusType)
                {
                    case GestureStatus.Started:
                        startWidth = column.ActualWidth;
                        dragging = true;
                        break;
                    case GestureStatus.Running when dragging:
                        column.Resize(startWidth + args.TotalX);
                        break;
                    case GestureStatus.Canceled when dragging:
                        column.Resize(startWidth);
                        dragging = false;
                        break;
                    case GestureStatus.Completed:
                        dragging = false;
                        break;
                }
            };
            handle.GestureRecognizers.Add(pan);
        }

        cell.Children.Add(handle);
        return cell;
    }

    private static Label CreateLabel(DataTableColumn column)
    {
        var label = new Label
        {
            FontSize = 13,
            Padding = new Thickness(8, 0, 12, 0),
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = column.TextAlignment,
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 1,
            InputTransparent = true,
        };
        label.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#202020"), Color.FromArgb("#EEEEEE"));
        return label;
    }

    private void OnTapped(object? sender, TappedEventArgs args)
    {
        ReportRowTap();
    }

    private void ReportRowTap()
    {
        _table.ReportTap(BindingContext);
    }

    internal void RefreshGeometry()
    {
        HeightRequest = _isHeader ? _table.HeaderHeight : _table.RowHeight;
        MinimumHeightRequest = HeightRequest;
        InvalidateMeasure();

        if (Handler is null || _arrangedBounds.Width <= 0)
        {
            return;
        }

        // AppKit currently does not schedule a layout for an already-realized item when only its
        // children change width. Run the child layout explicitly; do not reload ItemsSource, move
        // the native row frame, or lose selection/scroll position. This only visits live views.
        var layout = (Microsoft.Maui.ILayout)this;
        layout.CrossPlatformMeasure(_arrangedBounds.Width, HeightRequest);
        layout.CrossPlatformArrange(_arrangedBounds);
    }

    protected override ILayoutManager CreateLayoutManager()
    {
        return new RowLayoutManager(this, _table);
    }

    private sealed class RowLayoutManager : ILayoutManager
    {
        private readonly DataTableRow _row;
        private readonly DataTable _table;

        internal RowLayoutManager(DataTableRow row, DataTable table)
        {
            _row = row;
            _table = table;
        }

        public Size Measure(double widthConstraint, double heightConstraint)
        {
            var width = 0d;
            for (var i = 0; i < _row.Children.Count; i++)
            {
                var columnWidth = _table.Columns[i].ActualWidth;
                _row.Children[i].Measure(columnWidth, _row.HeightRequest);
                width += columnWidth;
            }

            return new Size(width, _row.HeightRequest);
        }

        public Size ArrangeChildren(Rect bounds)
        {
            _row._arrangedBounds = bounds;
            var x = bounds.X;
            for (var i = 0; i < _row.Children.Count; i++)
            {
                var width = _table.Columns[i].ActualWidth;
                _row.Children[i].Arrange(new Rect(x, bounds.Y, width, bounds.Height));
                x += width;
            }

            return new Size(Math.Max(bounds.Width, x - bounds.X), bounds.Height);
        }
    }
}