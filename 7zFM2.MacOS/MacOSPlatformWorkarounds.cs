using System.Runtime.CompilerServices;
using Microsoft.Maui.Platforms.MacOS.Handlers;
using SevenZip.FileManager2.Platform;

namespace SevenZip.FileManager2.MacOS;

/// <summary>
/// The backend defects this head works around.
/// </summary>
internal sealed class MacOSPlatformWorkarounds : IPlatformWorkarounds
{
    private static readonly ConditionalWeakTable<Page, object> SafeAreaOwners = new();

    /// <summary>
    /// Let the page's root layout consume the window safe area, not each nested layout as well.
    /// </summary>
    /// <remarks>
    /// The pinned AppKit backend measures newly attached layouts while their native frames still
    /// overlap the titlebar. SizeThatFits adds that transient inset to each layout's desired height,
    /// inflating the Auto rows. Shell modal navigation recreates these handlers, so a one-time
    /// window resize cannot fix subsequent dialogs. For registered pages, this LayoutHandler
    /// mapping sets IgnorePlatformSafeArea only on layouts below ContentPage.Content. It changes
    /// neither the page handler nor the root layout. The root consumes the native inset once;
    /// its descendants measure and arrange without adding it again. The mapping applies before
    /// measurement for every new layout handler, including replacements. Leaving only the page
    /// handler's flag unchanged does not itself inset the content.
    /// Leave dialogs and unrelated pages alone. Weak keys do not retain closed pages/windows.
    /// Delete when the backend correctly handles nested safe areas on initial attachment.
    /// </remarks>
    static MacOSPlatformWorkarounds()
    {
        LayoutHandler.Mapper.AppendToMapping("FileManagerNestedSafeArea", (handler, layout) =>
        {
            for (var element = layout as Element; element is not null; element = element.Parent)
            {
                if (element is Page page)
                {
                    if (SafeAreaOwners.TryGetValue(page, out _) &&
                        page is ContentPage contentPage && !ReferenceEquals(contentPage.Content, layout))
                    {
                        handler.PlatformView.IgnorePlatformSafeArea = true;
                    }

                    break;
                }
            }
        });
    }

    public void OnPageCreated(Page page)
    {
        SafeAreaOwners.GetOrCreateValue(page);
    }
}
