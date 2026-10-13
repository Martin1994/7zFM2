using SevenZip.FileManager2.Platform;

namespace SevenZip.FileManager2.Windows;

/// <summary>
/// No backend defect is worked around on this head yet - add them here, not in the pages.
/// </summary>
internal sealed class WindowsPlatformWorkarounds : IPlatformWorkarounds
{
    public void OnPageLayout(Page page)
    {
    }
}
