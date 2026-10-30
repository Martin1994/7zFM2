using SkiaSharp;

namespace SevenZip.FileManager2.Build;

/// <summary>Shared original-color SVG rendering, without theme processing or intermediate upscaling.</summary>
internal static class SvgRasterizer
{
    public static SKImage Render(SKPicture picture, int pixels)
    {
        SKRect bounds = picture.CullRect;
        if (!float.IsFinite(bounds.Width) || !float.IsFinite(bounds.Height) || bounds.Width <= 0 || bounds.Height <= 0)
        {
            throw new InvalidDataException("SVG has invalid or empty bounds.");
        }
        if (pixels is < 1 or > 4096)
        {
            throw new ArgumentOutOfRangeException(nameof(pixels));
        }

        var info = new SKImageInfo(pixels, pixels, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info)
            ?? throw new InvalidOperationException("Cannot allocate image surface.");
        surface.Canvas.Clear(SKColors.Transparent);
        float factor = Math.Min(pixels / bounds.Width, pixels / bounds.Height);
        surface.Canvas.Translate((pixels - bounds.Width * factor) / 2, (pixels - bounds.Height * factor) / 2);
        surface.Canvas.Scale(factor);
        surface.Canvas.Translate(-bounds.Left, -bounds.Top);
        surface.Canvas.DrawPicture(picture);
        return surface.Snapshot();
    }
}