using SevenZip.FileManager2.Platform;

namespace SevenZip.FileManager2.Windows;

/// <summary>
/// No-op implementation for the officially supported WinUI backend.
/// </summary>
/// <remarks>
/// Unlike the experimental MAUI Labs heads, this head keeps no backend workarounds by project
/// policy. Windows-specific behaviour and styling belong in controls, handlers or platform services.
/// </remarks>
internal sealed class WindowsPlatformWorkarounds : IPlatformWorkarounds
{
    public void OnPageCreated(Page page)
    {
    }
}
