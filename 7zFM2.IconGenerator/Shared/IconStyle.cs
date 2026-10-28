namespace SevenZip.FileManager2.IconGenerator;

/// <summary>
/// Shared design language. Coordinates are SVG units, not output pixels.
/// The stem angle is measured from downward vertical toward the left.
/// </summary>
public sealed record IconStyle
{
    public double Size { get; init; } = 256;
    public double CornerRadius { get; init; } = 48;
    public string Background { get; init; } = Oklch(0.8757, 0.1617, 90.3);
    public string Foreground { get; init; } = Oklch(1, 0, 0);
    public double SevenAngleDegrees { get; init; } = 32;
    public SevenProportions SevenProportions { get; init; } = new();

    /// <summary>
    /// Intentional background gap at zipper-to-solid joins, measured along the zipper axis
    /// as a fraction of the full two-column zipper width. Teeth do not get internal gaps.
    /// </summary>
    public double ZipperJoinGapRatio { get; init; } = 0.25;

    /// <summary>
    /// Converts OKLCH (D65) to an opaque sRGB color in uppercase #RRGGBB notation.
    /// Out-of-gamut colors are clipped per RGB channel, not perceptually gamut-mapped.
    /// </summary>
    /// <param name="lightness">OKLab lightness in [0, 1], not a percentage.</param>
    /// <param name="chroma">Nonnegative OKLab chroma (not a percentage).</param>
    /// <param name="hueDegrees">Hue in degrees; finite values wrap around 360, including negative angles.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// An input is nonfinite, lightness or chroma is out of range, or chroma is too large to convert.
    /// </exception>
    public static string Oklch(double lightness, double chroma, double hueDegrees)
    {
        if (!double.IsFinite(lightness) || lightness is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(lightness), "Must be finite and between 0 and 1.");
        }
        if (!double.IsFinite(chroma) || chroma < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chroma), "Must be finite and nonnegative.");
        }
        if (!double.IsFinite(hueDegrees))
        {
            throw new ArgumentOutOfRangeException(nameof(hueDegrees), "Must be finite.");
        }

        // Polar OKLCH -> Cartesian OKLab.
        double hue = (hueDegrees % 360) * Math.PI / 180;
        double a = chroma * Math.Cos(hue);
        double b = chroma * Math.Sin(hue);

        // Inverse OKLab transform: cube-root LMS -> LMS -> linear-light sRGB.
        double l = lightness + 0.3963377774 * a + 0.2158037573 * b;
        double m = lightness - 0.1055613458 * a - 0.0638541728 * b;
        double s = lightness - 0.0894841775 * a - 1.2914855480 * b;
        l = l * l * l;
        m = m * m * m;
        s = s * s * s;

        double red = 4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s;
        double green = -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s;
        double blue = -0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s;
        return $"#{ToSrgbByte(red):X2}{ToSrgbByte(green):X2}{ToSrgbByte(blue):X2}";

        static byte ToSrgbByte(double linear)
        {
            if (!double.IsFinite(linear))
            {
                throw new ArgumentOutOfRangeException(nameof(chroma), "Chroma is too large to convert.");
            }
            linear = Math.Clamp(linear, 0, 1);
            double encoded = linear <= 0.0031308
                ? 12.92 * linear
                : 1.055 * Math.Pow(linear, 1 / 2.4) - 0.055;
            return (byte)Math.Round(encoded * 255, MidpointRounding.AwayFromZero);
        }
    }

    internal void Validate()
    {
        Geometry.Positive(Size, nameof(Size));
        Geometry.Nonnegative(CornerRadius, nameof(CornerRadius));
        Geometry.Nonnegative(SevenAngleDegrees, nameof(SevenAngleDegrees));
        Geometry.Nonnegative(ZipperJoinGapRatio, nameof(ZipperJoinGapRatio));
        ArgumentNullException.ThrowIfNull(SevenProportions);
        if (CornerRadius > Size / 2 || SevenAngleDegrees >= 80)
        {
            throw new ArgumentException("Radius must not exceed half the canvas; stem angle must be below 80 degrees.");
        }
        foreach (string color in new[] { Background, Foreground })
        {
            if (color is null || color.Length != 7 || color[0] != '#' || !color.Skip(1).All(Uri.IsHexDigit))
            {
                throw new ArgumentException("Colors must use six-digit hexadecimal notation.");
            }
        }
    }
}