namespace SevenZip.FileManager2.IconGenerator;

public readonly record struct SvgPoint(double X, double Y);

internal static class Geometry
{
    public static void Nonnegative(double value, string name)
    {
        if (!double.IsFinite(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(name, "Must be finite and nonnegative.");
        }
    }

    public static void Positive(double value, string name)
    {
        Nonnegative(value, name);
        if (value == 0)
        {
            throw new ArgumentOutOfRangeException(name, "Must be positive.");
        }
    }

    public static void Inside(IEnumerable<SvgPoint> points, double size, string name)
    {
        if (points.Any(p => !double.IsFinite(p.X) || !double.IsFinite(p.Y)
            || p.X < -0.0001 || p.Y < -0.0001 || p.X > size + 0.0001 || p.Y > size + 0.0001))
        {
            throw new ArgumentException($"{name} exceeds the canvas; adjust its position or dimensions.");
        }
    }
}