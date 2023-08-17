using SevenZip.FileManager2.Formats;

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
            _valueBlock.Text = value.ToString("N0");
        }
    }

    public ulong SizeValue
    {
        set
        {
            _valueBlock.Text = SizeFormatter.FromBytes(value);
        }
    }

    public PropertyBlock()
    {
        InitializeComponent();
    }
}

