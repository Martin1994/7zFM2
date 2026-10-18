using Microsoft.Maui.Platforms.Linux.Gtk4.Platform;
using SevenZip.FileManager2.Controls;

namespace SevenZip.FileManager2.Linux;

/// <summary>Native viewport metrics and positioning for the pinned GTK layout handlers.</summary>
internal sealed class GtkDataTablePlatformLayout : IDataTablePlatformLayout
{
    public Size GetViewportSize(ScrollView scroll)
    {
        if (scroll.Handler?.PlatformView is not Gtk.ScrolledWindow native)
        {
            return new Size(scroll.Width, scroll.Height);
        }

        return new Size(
            Math.Max(0, scroll.Width - GetInset(native.GetWidth(), native.GetHadjustment())),
            Math.Max(0, scroll.Height - GetInset(native.GetHeight(), native.GetVadjustment())));
    }

    public double GetRowViewportWidth(CollectionView list)
    {
        return list.Handler?.PlatformView is Gtk.ScrolledWindow native
            ? Math.Max(0, list.Width - GetInset(native.GetWidth(), native.GetHadjustment()))
            : list.Width;
    }

    private static double GetInset(int allocatedSize, Gtk.Adjustment adjustment)
    {
        var pageSize = adjustment.GetPageSize();
        return allocatedSize > 0 && pageSize > 0 ? Math.Max(0, allocatedSize - pageSize) : 0;
    }

    public void SetHorizontalScrollOffset(CollectionView list, double offset)
    {
        if (list.Handler?.PlatformView is Gtk.Widget native && native.GetParent() is GtkLayoutPanel parent)
        {
            // The pinned CollectionViewHandler bypasses the base arrange cache used by
            // TranslationX. Move only the child position, without reloading/rearranging the list.
            parent.MoveChild(native, list.Frame.X + offset, list.Frame.Y);
        }
    }

    public void RefreshRow(Layout row, double width)
    {
        if (row.Handler?.PlatformView is not GtkLayoutPanel native || native.GetParent() is null)
        {
            return;
        }

        // Template inflation pins the old viewport width. Update that request on resize, but
        // leave native Y/height allocation and scrolling to Gtk.ListView.
        native.SetSizeRequest((int)Math.Ceiling(width), (int)Math.Ceiling(row.HeightRequest));
        var layout = (Microsoft.Maui.ILayout)row;
        layout.CrossPlatformMeasure(width, row.HeightRequest);
        layout.CrossPlatformArrange(new Rect(0, 0, width, row.HeightRequest));
    }

    public IDisposable? ObserveViewport(ScrollView scroll, CollectionView list, Action changed)
    {
        return scroll.Handler?.PlatformView is Gtk.ScrolledWindow outer &&
            list.Handler?.PlatformView is Gtk.ScrolledWindow inner
                ? new ViewportObserver(outer, inner, changed)
                : null;
    }

    private sealed class ViewportObserver : IDisposable
    {
        private readonly Gtk.Adjustment[] _adjustments;
        private readonly Action _changed;
        private double[] _sizes;

        public ViewportObserver(Gtk.ScrolledWindow outer, Gtk.ScrolledWindow inner, Action changed)
        {
            _adjustments = [outer.GetHadjustment(), outer.GetVadjustment(), inner.GetHadjustment()];
            _changed = changed;
            _sizes = _adjustments.Select(a => a.GetPageSize()).ToArray();
            foreach (var adjustment in _adjustments)
            {
                adjustment.OnChanged += OnChanged;
            }
        }

        private void OnChanged(Gtk.Adjustment sender, EventArgs args)
        {
            var sizes = _adjustments.Select(a => a.GetPageSize()).ToArray();
            if (!_sizes.SequenceEqual(sizes))
            {
                _sizes = sizes;
                _changed();
            }
        }

        public void Dispose()
        {
            foreach (var adjustment in _adjustments)
            {
                adjustment.OnChanged -= OnChanged;
            }
        }
    }
}