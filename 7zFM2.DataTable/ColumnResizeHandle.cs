namespace SevenZip.FileManager2.Controls;

/// <summary>
/// A thin separator drawn inside a wider mouse target. The visual and pan gesture share one
/// native surface, avoiding decorative child views intercepting the drag on some backends.
/// </summary>
internal sealed class ColumnResizeHandle : GraphicsView, IDrawable
{
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(
        nameof(Color), typeof(Color), typeof(ColumnResizeHandle), Colors.Transparent,
        propertyChanged: static (view, _, _) => ((ColumnResizeHandle)view).Invalidate());

    public Color Color
    {
        get => (Color)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public ColumnResizeHandle()
    {
        BackgroundColor = Colors.Transparent;
        Drawable = this;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = Color;
        canvas.FillRectangle((dirtyRect.Width - 1) / 2, 0, 1, dirtyRect.Height);
    }
}