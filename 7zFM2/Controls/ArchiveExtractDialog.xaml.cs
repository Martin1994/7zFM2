using SevenZip.FileManager2.ViewModels;

namespace SevenZip.FileManager2.Controls;

public sealed partial class ArchiveExtractDialog : ContentDialog
{
    public ArchiveExtractViewModel? ArchiveExtract { get; set; }

    public ArchiveExtractDialog()
    {
        InitializeComponent();
    }
}
