// MAUI's implicit usings bring in System.Collections.Generic but not System.Collections, so a bare
// "IEnumerable" would resolve to the generic type.
using System.Collections;

namespace SevenZip.FileManager2.DataTable;

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
/// The spike measured that a MAUI-controls-per-row table costs roughly 600 us per extra control
/// per row and tops out near 18 fps on a full-viewport scroll (<c>spike/README.md</c>, Gate D), so
/// the likely end state is one hand-drawn surface that owns its own scrolling. Keeping the app
/// behind this type means that can be swapped without touching the page.
/// </para>
/// </remarks>
public class FileTable : ContentView
{
    private readonly CollectionView _list = new();

    public FileTable()
    {
        _list.SelectionChanged += OnListSelectionChanged;
        Content = _list;
    }

    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource),
        typeof(IEnumerable),
        typeof(FileTable),
        propertyChanged: (bindable, _, value) => ((FileTable)bindable)._list.ItemsSource = (IEnumerable?)value);

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create(
        nameof(ItemTemplate),
        typeof(DataTemplate),
        typeof(FileTable),
        propertyChanged: (bindable, _, value) => ((FileTable)bindable)._list.ItemTemplate = (DataTemplate?)value);

    public DataTemplate? ItemTemplate
    {
        get => (DataTemplate?)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public static readonly BindableProperty SelectionModeProperty = BindableProperty.Create(
        nameof(SelectionMode),
        typeof(SelectionMode),
        typeof(FileTable),
        SelectionMode.Single,
        propertyChanged: (bindable, _, value) => ((FileTable)bindable)._list.SelectionMode = (SelectionMode)value);

    public SelectionMode SelectionMode
    {
        get => (SelectionMode)GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }

    public static readonly BindableProperty SelectedItemProperty = BindableProperty.Create(
        nameof(SelectedItem),
        typeof(object),
        typeof(FileTable),
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
        var table = (FileTable)bindable;

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
