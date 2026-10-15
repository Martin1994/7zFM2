using CommunityToolkit.Maui;

namespace SevenZip.FileManager2.Controls;

/// <summary>A shared column definition. Widths are device-independent units, not pixels.</summary>
public sealed partial class DataTableColumn : BindableObject
{
    [BindableProperty]
    public partial string Header { get; set; } = "";

    [BindableProperty]
    public partial string BindingPath { get; set; } = ".";

    [BindableProperty]
    public partial string? StringFormat { get; set; }

    [BindableProperty]
    public partial DataTemplate? CellTemplate { get; set; }

    [BindableProperty]
    public partial TextAlignment TextAlignment { get; set; } = TextAlignment.Start;

    [BindableProperty]
    public partial bool CanResize { get; set; } = true;

    // Keep invalid values out of layout, including values supplied by bindings rather than a drag.
    public static readonly BindableProperty WidthProperty = BindableProperty.Create(
        nameof(Width), typeof(double), typeof(DataTableColumn), 160d,
        defaultBindingMode: BindingMode.TwoWay,
        validateValue: IsPositiveFinite);

    public static readonly BindableProperty MinWidthProperty = BindableProperty.Create(
        nameof(MinWidth), typeof(double), typeof(DataTableColumn), 40d,
        validateValue: IsPositiveFinite);

    public static readonly BindableProperty MaxWidthProperty = BindableProperty.Create(
        nameof(MaxWidth), typeof(double), typeof(DataTableColumn), double.PositiveInfinity,
        validateValue: IsPositive);

    public double Width
    {
        get
        {
            return (double)GetValue(WidthProperty);
        }
        set
        {
            SetValue(WidthProperty, value);
        }
    }

    public double MinWidth
    {
        get
        {
            return (double)GetValue(MinWidthProperty);
        }
        set
        {
            SetValue(MinWidthProperty, value);
        }
    }

    public double MaxWidth
    {
        get
        {
            return (double)GetValue(MaxWidthProperty);
        }
        set
        {
            SetValue(MaxWidthProperty, value);
        }
    }

    /// <summary>MinWidth wins if conflicting bounds are supplied.</summary>
    public double ActualWidth
    {
        get
        {
            return Math.Clamp(Width, MinWidth, Math.Max(MinWidth, MaxWidth));
        }
    }

    internal void Resize(double width)
    {
        if (double.IsFinite(width))
        {
            Width = Math.Clamp(width, MinWidth, Math.Max(MinWidth, MaxWidth));
        }
    }

    private static bool IsPositiveFinite(BindableObject bindable, object value)
    {
        return value is double number && double.IsFinite(number) && number > 0;
    }

    private static bool IsPositive(BindableObject bindable, object value)
    {
        return value is double number && number > 0;
    }
}