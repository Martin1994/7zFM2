using System.Xml.Linq;

namespace SevenZip.FileManager2.IconGenerator;

/// <summary>A solid seven beside an upper-half zipper and a square solid bottom stop.</summary>
public static class Icon2
{
    public const string FileName = "icon2.svg";

    public static SevenParameters DefaultSeven { get; } = new(
        Left: 32, Top: 68, TopWidth: 107);
    public static Zipper DefaultZipper { get; } = new(ToothCount: 6, ToothHeight: 18);
    public const double DefaultBaseline = 188;
    public const double DefaultZipperLeft = 160;
    public const double DefaultZipperTop = 0;
    public const double DefaultZipperWidth = 48;
    public const double DefaultStopHeight = 32;

    /// <summary>Adding teeth extends downward; the solid stop follows after an intentional background gap.</summary>
    public static string Render(IconStyle? style = null, SevenParameters? parameters = null,
        Zipper? zipper = null, double baseline = DefaultBaseline, double left = DefaultZipperLeft,
        double top = DefaultZipperTop, double width = DefaultZipperWidth, double stopHeight = DefaultStopHeight)
    {
        style ??= new();
        parameters ??= DefaultSeven;
        zipper ??= DefaultZipper;
        var seven = new Seven(style, parameters);
        Geometry.Nonnegative(left, nameof(left));
        Geometry.Nonnegative(top, nameof(top));
        XElement[] teeth = zipper.Cells(left, top, width);
        double gap = width * style.ZipperJoinGapRatio;
        XElement stop = zipper.Stop(left, top, width, stopHeight, gap);
        Geometry.Inside([new(left, top), new(left + width, top + zipper.Length + gap + stopHeight)], style.Size, "icon2 zipper");
        var toothGroup = new XElement(SvgDocument.Namespace + "g", new XAttribute("data-role", "zipper"), teeth, stop);
        return SvgDocument.Create(style, "icon2",
            $"A solid seven beside {zipper.ToothCount} alternating zipper blocks, separated from a solid rectangular bottom stop by an intentional background gap. No center rail or hollow pull tab.",
            "Icon2.cs", seven.Element(seven.LengthToBaseline(baseline), TailCut.Horizontal), toothGroup);
    }
}