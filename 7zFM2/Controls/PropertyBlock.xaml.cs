namespace SevenZip.FileManager2.Controls;

public partial class PropertyBlock
{
    public string Title { get; set; } = "";

    public string TextValue
    {
        set
        {
            _valueBlock.Text = value;
        }
    }

    public double PercentageValue
    {
        set
        {
            _valueBlock.Text = value.ToString("P2");
        }
    }

    public ulong CountValue
    {
        set
        {
            _valueBlock.Text = value.ToString("n0");
        }
    }

    public ulong SizeValue
    {
        set
        {
            _valueBlock.Text = FormatSize(value);
        }
    }

    public PropertyBlock()
    {
        InitializeComponent();
    }

    private static readonly string[] SIZE_UNITS = new[] { "bytes", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB" };
    private static string FormatSize(ulong bytes)
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

