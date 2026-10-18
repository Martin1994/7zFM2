using AppKit;
using CoreGraphics;
using Foundation;
using Microsoft.Maui.Platforms.MacOS.Platform;
using SevenZip.FileManager2.Controls;

namespace SevenZip.FileManager2.MacOS;

/// <summary>Workaround for the CollectionView geometry bugs in MAUI Labs preview.12.26474.1.</summary>
public sealed class MacOSDataTablePlatformLayout : IDataTablePlatformLayout
{
    public Size GetViewportSize(ScrollView scroll)
    {
        if (scroll.Handler?.PlatformView is not NSScrollView native)
        {
            return new Size(scroll.Width, scroll.Height);
        }

        return new Size(
            Math.Max(0, scroll.Width - Math.Max(0, (double)native.Frame.Width - (double)native.ContentView.Bounds.Width)),
            Math.Max(0, scroll.Height - Math.Max(0, (double)native.Frame.Height - (double)native.ContentView.Bounds.Height)));
    }

    public double GetRowViewportWidth(CollectionView list)
    {
        return list.Handler?.PlatformView is NSScrollView native
            ? Math.Max(0, list.Width - Math.Max(0, (double)native.Frame.Width - (double)native.ContentView.Bounds.Width))
            : list.Width;
    }

    public IDisposable? ObserveViewport(ScrollView scroll, CollectionView list, Action changed)
    {
        return scroll.Handler?.PlatformView is NSScrollView outer && list.Handler?.PlatformView is NSScrollView inner
            ? new ViewportObserver(outer.ContentView, inner.ContentView, changed)
            : null;
    }

    private sealed class ViewportObserver : IDisposable
    {
        private readonly NSObject _outerObserver;
        private readonly NSObject _innerObserver;

        public ViewportObserver(NSClipView outer, NSClipView inner, Action changed)
        {
            outer.PostsFrameChangedNotifications = true;
            inner.PostsFrameChangedNotifications = true;
            _outerObserver = NSNotificationCenter.DefaultCenter.AddObserver(NSView.FrameChangedNotification, _ => changed(), outer);
            _innerObserver = NSNotificationCenter.DefaultCenter.AddObserver(NSView.FrameChangedNotification, _ => changed(), inner);
        }

        public void Dispose()
        {
            NSNotificationCenter.DefaultCenter.RemoveObserver(_outerObserver);
            NSNotificationCenter.DefaultCenter.RemoveObserver(_innerObserver);
            _outerObserver.Dispose();
            _innerObserver.Dispose();
        }
    }

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