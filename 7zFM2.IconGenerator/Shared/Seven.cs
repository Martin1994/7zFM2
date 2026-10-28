using System.Xml.Linq;

namespace SevenZip.FileManager2.IconGenerator;

/// <summary>Per-icon placement and scale only; glyph proportions cannot drift between icons.</summary>
public sealed record SevenParameters(double Left, double Top, double TopWidth);

/// <summary>
/// One reference glyph, scaled uniformly by TopWidth / ReferenceWidth in both icons.
/// ShoulderDrop is the right corner's vertical thickness; StemWidth is perpendicular
/// to the slanted stem. Change these shared dimensions rather than per-icon geometry.
/// </summary>
public sealed record SevenProportions
{
    public double ReferenceWidth { get; init; } = 107;
    public double BarHeight { get; init; } = 30;
    public double ShoulderDrop { get; init; } = 26;
    public double StemWidth { get; init; } = 36;
}

public enum TailCut
{
    AlongToothGrid,
    Horizontal
}

/// <summary>A common seven constructed from a top bar and two parallel slanted edges.</summary>
public sealed class Seven
{
    private readonly IconStyle style;
    private readonly SevenParameters parameters;
    private readonly double sin;
    private readonly double cos;
    private readonly double right;
    private readonly double shoulderY;
    private readonly SvgPoint innerCorner;
    private readonly double minimumLength;

    public SvgPoint Origin { get; }
    public double StemWidth { get; }
    public double BarHeight { get; }
    public double ShoulderDrop { get; }

    public Seven(IconStyle style, SevenParameters parameters)
    {
        style.Validate();
        Geometry.Nonnegative(parameters.Left, nameof(parameters.Left));
        Geometry.Nonnegative(parameters.Top, nameof(parameters.Top));
        Geometry.Positive(parameters.TopWidth, nameof(parameters.TopWidth));
        SevenProportions proportions = style.SevenProportions;
        Geometry.Positive(proportions.ReferenceWidth, nameof(proportions.ReferenceWidth));
        Geometry.Positive(proportions.BarHeight, nameof(proportions.BarHeight));
        Geometry.Positive(proportions.ShoulderDrop, nameof(proportions.ShoulderDrop));
        Geometry.Positive(proportions.StemWidth, nameof(proportions.StemWidth));
        double scale = parameters.TopWidth / proportions.ReferenceWidth;
        BarHeight = proportions.BarHeight * scale;
        ShoulderDrop = proportions.ShoulderDrop * scale;
        StemWidth = proportions.StemWidth * scale;
        if (ShoulderDrop > BarHeight)
        {
            throw new ArgumentException("ShoulderDrop must not exceed BarHeight.");
        }

        this.style = style;
        this.parameters = parameters;
        double radians = style.SevenAngleDegrees * Math.PI / 180;
        sin = Math.Sin(radians);
        cos = Math.Cos(radians);
        right = parameters.Left + parameters.TopWidth;
        shoulderY = parameters.Top + ShoulderDrop;
        Origin = new(right - StemWidth * cos / 2, shoulderY - StemWidth * sin / 2);
        double innerY = parameters.Top + BarHeight;
        innerCorner = new(right - (innerY - shoulderY) * Math.Tan(radians) - StemWidth / cos, innerY);
        if (innerCorner.X <= parameters.Left)
        {
            throw new ArgumentException("The stem consumes the entire top bar; reduce StemWidth or BarHeight.");
        }
        minimumLength = (innerY - shoulderY + StemWidth * sin) / cos;
    }

    /// <summary>Local x runs across the stem; local y runs down its slanted axis.</summary>
    public SvgPoint Point(double x, double y)
    {
        return new(Origin.X + x * cos - y * sin, Origin.Y + x * sin + y * cos);
    }

    public double LengthToBaseline(double baseline)
    {
        Geometry.Positive(baseline, nameof(baseline));
        return (baseline - shoulderY) / cos;
    }

    /// <summary>Length follows the right edge from the elbow; the tail may be slanted or horizontal.</summary>
    public string Path(double length, TailCut cut)
    {
        Geometry.Positive(length, nameof(length));
        if (length <= minimumLength)
        {
            throw new ArgumentException("Not enough solid stem below the top bar; reduce teeth or increase the stem length.");
        }
        SvgPoint outerTail = Point(StemWidth / 2, length);
        SvgPoint innerTail = cut switch
        {
            TailCut.Horizontal => new(outerTail.X - StemWidth / cos, outerTail.Y),
            TailCut.AlongToothGrid => Point(-StemWidth / 2, length),
            _ => throw new ArgumentOutOfRangeException(nameof(cut))
        };
        SvgPoint[] points = [new(parameters.Left, parameters.Top), new(right, parameters.Top),
            new(right, shoulderY), outerTail, innerTail, innerCorner, new(parameters.Left, innerCorner.Y)];
        Geometry.Inside(points, style.Size, "Seven");
        return "M" + string.Join("L", points.Select(p => $"{SvgDocument.Number(p.X)} {SvgDocument.Number(p.Y)}")) + "Z";
    }

    public XElement Element(double length, TailCut cut)
    {
        return new XElement(SvgDocument.Namespace + "path",
            new XAttribute("data-role", "seven"), new XAttribute("d", Path(length, cut)));
    }
}