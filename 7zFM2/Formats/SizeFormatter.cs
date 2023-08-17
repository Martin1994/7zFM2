namespace SevenZip.FileManager2.Formats;

public class SizeFormatter
{
    private static readonly string[] SIZE_UNITS = new[] { "bytes", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB" };
    public static string FromBytes(ulong bytes)
    {
        string unit = SIZE_UNITS[0];
        double magnitude = bytes;
        for (int i = 0; i < SIZE_UNITS.Length - 1 && magnitude >= 1024; i++)
        {
            magnitude /= 1024;
            unit = SIZE_UNITS[i + 1];
        }
        return $"{magnitude:0.##} {unit}";
    }
}
