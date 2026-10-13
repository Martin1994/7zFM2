using SevenZip.FileManager2.Platform;

namespace SevenZip.FileManager2.MacOS;

/// <summary>
/// The backend defects this head works around.
/// </summary>
internal sealed class MacOSPlatformWorkarounds : IPlatformWorkarounds
{
    private CancellationTokenSource? _settle;
    private bool _applied;

    public void OnPageLayout(Page page)
    {
        if (_applied)
        {
            return;
        }

        // The workaround has to land after the last layout of the start-up burst, because that is the
        // pass whose measurement gets baked in - this backend lays the page out at 208x16 and 508x1
        // before the window's real size - so wait for the size to stop changing.
        _settle?.Cancel();

        var settle = _settle = new CancellationTokenSource();
        page.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(250), () =>
        {
            if (settle.IsCancellationRequested || _applied)
            {
                return;
            }

            _applied = true;
            FixInflatedRowHeights(page);
        });
    }

    /// <summary>
    /// One point of window height, and back: the programmatic equivalent of the resize a user does by
    /// hand to clear the defect.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The defect: auto-sized rows keep an inflated height from the first measure. This backend
    /// measures its containers while they still overlap the titlebar and folds the top safe-area inset
    /// (52pt here) into their desired height, and nothing ever re-measures them - a cross-platform
    /// <c>InvalidateMeasure</c> does not reach AppKit on this backend, so only a real size change drives
    /// a layout pass.
    /// </para>
    /// <para>
    /// The fingerprint: the three rows come out 83/50/78pt tall instead of 32/32/26, which shows up as
    /// blank bands above and below the content and a path <c>Entry</c> three lines high. The leaves are
    /// never wrong - only the rows' cached heights are.
    /// </para>
    /// <para>
    /// Delete when the backend re-measures on invalidation, or when the pinned package carries the fix.
    /// Measurements: <c>docs/upstream-defects.md</c> #1.
    /// </para>
    /// </remarks>
    private static void FixInflatedRowHeights(Page page)
    {
        if (page.Window is not { } window || page.Height <= 0)
        {
            return;
        }

        var height = page.Height;
        window.Height = height - 1;
        page.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(200), () => window.Height = height);
    }
}
