using CoreGraphics;
using Microsoft.Maui.Platforms.MacOS.Platform;
using SevenZip.FileManager2.Controls;

namespace SevenZip.FileManager2.MacOS;

/// <summary>Workaround for the CollectionView geometry bugs in MAUI Labs preview.12.26474.1.</summary>
public sealed class MacOSDataTablePlatformLayout : IDataTablePlatformLayout
{
    public void RefreshRow(Layout row, double width)
    {
        if (row.Handler?.PlatformView is not MacOSContainerView native || native.Superview is null)
        {
            return;
        }

        // The pinned handler only positions newly realized items. Existing items retain their old
        // width after a viewport resize. It also assigns native Frame without setting MAUI Frame.
        // Preserve the native Y/height: the virtualizing handler owns those, not our cell layout.
        var frame = native.Frame;
        native.Frame = new CGRect(frame.X, frame.Y, width, frame.Height);
        var bounds = native.Bounds;
        var layout = (Microsoft.Maui.ILayout)row;
        layout.CrossPlatformMeasure(bounds.Width, bounds.Height);
        layout.CrossPlatformArrange(new Rect(0, 0, bounds.Width, bounds.Height));
    }
}