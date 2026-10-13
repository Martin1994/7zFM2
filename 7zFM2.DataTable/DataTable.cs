// MAUI's implicit usings bring in System.Collections.Generic but not System.Collections, so a bare
// "IEnumerable" would resolve to the generic type.
using System.Collections;
using CommunityToolkit.Maui;

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
public partial class DataTable : ContentView
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

    /// <summary>
    /// The items to render.
    /// </summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnItemsSourceChanged))]
    public partial IEnumerable? ItemsSource { get; set; }

    /// <summary>
    /// The row template.
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
        ((DataTable)bindable)._list.ItemsSource = (IEnumerable?)newValue;
    }

    private static void OnItemTemplateChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((DataTable)bindable)._list.ItemTemplate = (DataTemplate?)newValue;
    }

    private static void OnSelectionModeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((DataTable)bindable)._list.SelectionMode = (SelectionMode)newValue;
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
