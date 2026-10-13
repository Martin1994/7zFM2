using SevenZip.FileManager2.Platform;

namespace SevenZip.FileManager2.Linux;

/// <summary>
/// No backend defect is worked around on this head yet - add them here, not in the pages.
/// </summary>
internal sealed class GtkPlatformWorkarounds : IPlatformWorkarounds
{
    public void OnPageLayout(Page page)
    {
    }
}
