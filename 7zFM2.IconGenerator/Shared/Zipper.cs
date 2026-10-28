using System.Xml.Linq;

namespace SevenZip.FileManager2.IconGenerator;

public enum ToothSide
{
    Left,
    Right
}

/// <summary>
/// One tooth is one row with a foreground half and an exposed-background half, not a pair.
/// FirstSide controls the alternating phase independently of odd/even tooth counts.
/// </summary>
public sealed record Zipper(int ToothCount, double ToothHeight, ToothSide FirstSide = ToothSide.Left)
{
    public double Length => ToothCount * ToothHeight;

    /// <summary>Two touching columns of alternating cells, with no center rail or surrounding stroke.</summary>
    public XElement[] Cells(double left, double top, double width)
    {
        Geometry.Positive(ToothHeight, nameof(ToothHeight));
        Geometry.Positive(width, nameof(width));
        if (ToothCount is < 1 or > 64 || !Enum.IsDefined(FirstSide))
        {
            throw new ArgumentException("Use 1–64 teeth and a valid first side.");
        }
        int phase = FirstSide == ToothSide.Left ? 0 : 1;
        return Enumerable.Range(0, ToothCount).Select(index =>
            SvgDocument.Rectangle(left + ((index + phase) % 2) * width / 2,
            top + index * ToothHeight, width / 2, ToothHeight, "tooth")).ToArray();
    }

    /// <summary>A short solid rectangular bottom stop, not a rounded or hollow pull tab.</summary>
    public XElement Stop(double left, double top, double width, double height, double gap = 0)
    {
        Geometry.Positive(height, nameof(height));
        Geometry.Positive(width, nameof(width));
        Geometry.Nonnegative(gap, nameof(gap));
        return SvgDocument.Rectangle(left, top + Length + gap, width, height, "stop");
    }
}