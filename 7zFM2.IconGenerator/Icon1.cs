using System.Xml.Linq;

namespace SevenZip.FileManager2.IconGenerator;

/// <summary>A mostly solid seven with a fixed-length, slanted zipper tail.</summary>
public static class Icon1
{
    public const string FileName = "icon1.svg";

    public static SevenParameters DefaultSeven { get; } = new(
        Left: 40, Top: 48, TopWidth: 176);
    public static Zipper DefaultZipper { get; } = new(ToothCount: 4, ToothHeight: 18);
    public const double DefaultStemLength = 174.4;

    /// <summary>
    /// More teeth replace solid stem upward, without changing total stem length.
    /// The tail follows the tooth grid instead of a horizontal baseline. A deliberate
    /// background gap separates the solid stem from the first tooth; changing the gap
    /// shortens the solid portion without moving the teeth.
    /// </summary>
    public static string Render(IconStyle? style = null, SevenParameters? parameters = null,
        Zipper? zipper = null, double stemLength = DefaultStemLength)
    {
        style ??= new();
        parameters ??= DefaultSeven;
        zipper ??= DefaultZipper;
        Geometry.Positive(stemLength, nameof(stemLength));
        var seven = new Seven(style, parameters);
        double start = stemLength - zipper.Length;
        double gap = seven.StemWidth * style.ZipperJoinGapRatio;
        XElement[] teeth = zipper.Cells(-seven.StemWidth / 2, start, seven.StemWidth);
        Geometry.Inside(new[] { -1, 1 }.SelectMany(side => new[] { start, stemLength }
            .Select(y => seven.Point(side * seven.StemWidth / 2, y))), style.Size, "icon1 zipper");
        var toothGroup = new XElement(SvgDocument.Namespace + "g",
            new XAttribute("data-role", "zipper"),
            new XAttribute("transform", $"translate({SvgDocument.Number(seven.Origin.X)} {SvgDocument.Number(seven.Origin.Y)}) rotate({SvgDocument.Number(style.SevenAngleDegrees)})"),
            teeth);
        return SvgDocument.Create(style, "icon1",
            $"A solid seven separated from {zipper.ToothCount} alternating zipper blocks by an intentional background gap. No center rail; the tail follows the slanted tooth grid.",
            "Icon1.cs", seven.Element(start - gap, TailCut.AlongToothGrid), toothGroup);
    }
}