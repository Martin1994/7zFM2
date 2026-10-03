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
        _list.SelectionChanged += OnListSelectionChanged;
        Content = _list;
    }

    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

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

    private void OnListSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        SelectedItem = e.CurrentSelection.Count > 0 ? e.CurrentSelection[0] : null;
        SelectionChanged?.Invoke(this, e);
    }
}
