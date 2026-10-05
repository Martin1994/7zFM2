// MAUI's implicit usings bring in System.Collections.Generic but not System.Collections, so a bare
// "IEnumerable" would resolve to the generic type.
using System.Collections;

namespace SevenZip.FileManager2.Controls;

/// <summary>
/// Virtualized row surface for the file manager.
/// </summary>
/// <remarks>
/// <para>
/// Hand-rolled: the available DataTable/DataGrid controls are built on UI-framework types
/// (<c>Panel</c>, <c>ContentControl</c>, <c>ContentSizer</c>, <c>ItemsStackPanel</c>) that MAUI does
/// not have, and CommunityToolkit.Maui ships no grid at all.
/// </para>
/// <para>
/// <b>Step 1 scope:</b> a plain, virtualized filename list. No header, no columns, no interaction
/// beyond selection.
/// </para>
/// <para>
/// This type is deliberately the seam between the app and whatever eventually renders the rows.
/// Per-row cost in MAUI is dominated by control count - measured at roughly 600 us per extra control
/// per row, which puts a three-control row at about 18 fps during a full-viewport scroll - so the
/// likely end state is one hand-drawn surface that owns its own scrolling. Keeping the app behind
/// this type means that can be swapped without touching the page.
/// </para>
/// </remarks>
public class DataTable : ContentView
{
    private readonly CollectionView _list = new();

    public DataTable()
    {
        // Apply the default before any items exist. The platform re-creates and re-measures item
        // views when the selection mode changes, and it attaches the row's selection gesture to
        // each item view as it is created - so the mode has to be right before the first reload.
        _list.SelectionMode = SelectionMode;
        _list.SelectionChanged += OnListSelectionChanged;
        Content = _list;
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

    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource),
        typeof(IEnumerable),
        typeof(DataTable),
        propertyChanged: (bindable, _, value) => ((DataTable)bindable)._list.ItemsSource = (IEnumerable?)value);

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create(
        nameof(ItemTemplate),
        typeof(DataTemplate),
        typeof(DataTable),
        propertyChanged: (bindable, _, value) => ((DataTable)bindable)._list.ItemTemplate = (DataTemplate?)value);

    public DataTemplate? ItemTemplate
    {
        get => (DataTemplate?)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public static readonly BindableProperty SelectionModeProperty = BindableProperty.Create(
        nameof(SelectionMode),
        typeof(SelectionMode),
        typeof(DataTable),
        SelectionMode.Single,
        propertyChanged: (bindable, _, value) => ((DataTable)bindable)._list.SelectionMode = (SelectionMode)value);

    public SelectionMode SelectionMode
    {
        get => (SelectionMode)GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }

    public static readonly BindableProperty SelectedItemProperty = BindableProperty.Create(
        nameof(SelectedItem),
        typeof(object),
        typeof(DataTable),
        null,
        BindingMode.TwoWay,
        propertyChanged: OnSelectedItemChanged);

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    private static void OnSelectedItemChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var table = (DataTable)bindable;

        // Guards the round-trip that OnListSelectionChanged starts.
        if (!ReferenceEquals(table._list.SelectedItem, newValue))
        {
            table._list.SelectedItem = newValue;
        }
    }

    private object? _lastTappedItem;
    private long _lastTapTicks;

    /// <summary>
    /// Records a tap on <paramref name="item"/>, reported by the row template's
    /// <c>TapGestureRecognizer</c>, and raises <see cref="ItemActivated"/> when the tap completes a
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
        var now = Environment.TickCount64;
        var isActivation = ReferenceEquals(item, _lastTappedItem)
            && now - _lastTapTicks <= (long)ActivationInterval.TotalMilliseconds;

        _lastTappedItem = isActivation ? null : item;
        _lastTapTicks = now;

        if (isActivation)
        {
            ItemActivated?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnListSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        SelectedItem = e.CurrentSelection.Count > 0 ? e.CurrentSelection[0] : null;
        SelectionChanged?.Invoke(this, e);
    }
}
