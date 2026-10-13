using System.Globalization;

namespace SevenZip.FileManager2.Formats;

/// <summary>
/// Renders a byte count as a size - the XAML face of <see cref="SizeFormatter" />.
/// </summary>
public sealed class ByteSizeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return "";
        }

        // `culture` parameter is ignored on purpose. MAUI sends CultureInfo.CurrentUICulture, which
        // should only be used for resource and asset lookups. Text should follow the user's locale,
        // which is CultureInfo.CurrentCulture.
        return SizeFormatter.FromBytes(System.Convert.ToDouble(value));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
