using SevenZip.FileManager2.ViewModels;

namespace SevenZip.FileManager2.Controls;

public sealed partial class ExtractDialog : ContentDialog
{
    public ArchiveExtract? ArchiveExtract { get; set; }

    public ExtractDialog()
    {
        InitializeComponent();
    }
}
