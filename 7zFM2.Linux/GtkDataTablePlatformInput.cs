using SevenZip.FileManager2.Controls;

namespace SevenZip.FileManager2.Linux;

/// <summary>
/// Bridges GTK row clicks to the shared table without relying on the backend's incomplete
/// TapGestureRecognizer support. Native CollectionView selection remains enabled.
/// </summary>
internal sealed class GtkDataTablePlatformInput : IDataTablePlatformInput
{
    public IDisposable? AttachRow(Layout row, Action<object?> reportTap)
    {
        return row.Handler?.PlatformView is Gtk.Widget widget
            ? new RowClicks(row, widget, reportTap)
            : null;
    }

    private sealed class RowClicks : IDisposable
    {
        private readonly Layout _row;
        private readonly Gtk.Widget _widget;
        private readonly Action<object?> _reportTap;
        private Gtk.Widget? _target;
        private Gtk.GestureClick? _click;
        private object? _pressedItem;
        private int _bindingVersion;
        private bool _disposed;

        public RowClicks(Layout row, Gtk.Widget widget, Action<object?> reportTap)
        {
            _row = row;
            _widget = widget;
            _reportTap = reportTap;
            _row.BindingContextChanged += OnBindingContextChanged;
            _widget.OnMap += OnMap;
            _widget.OnUnmap += OnUnmap;
            if (_widget.GetMapped())
            {
                AttachController();
            }
        }

        private void OnMap(Gtk.Widget sender, EventArgs args)
        {
            AttachController();
        }

        private void AttachController()
        {
            DetachController();
            // Observe before native selection can unbind/rebuild the row during release.
            // Do not subscribe after the selection gesture's own release callback: that callback
            // may unmap this row and disconnect us before our release handler gets a turn.
            _target = _widget.GetParent() ?? _widget;
            _click = Gtk.GestureClick.New();
            _click.SetButton(1);
            _click.SetPropagationPhase(Gtk.PropagationPhase.Capture);
            _click.OnPressed += OnPressed;
            _click.OnReleased += OnReleased;
            _click.OnStopped += OnStopped;
            _target.AddController(_click);

            // Selection can claim the next press in the same native click sequence. Group our
            // observer with its click recognizer so that claim does not suppress our release.
            // We still never claim or deny a sequence ourselves.
            var controllers = _target.ObserveControllers();
            for (uint index = 0; index < controllers.GetNItems(); index++)
            {
                if (controllers.GetObject(index) is Gtk.GestureClick nativeClick &&
                    nativeClick.Handle.DangerousGetHandle() != _click.Handle.DangerousGetHandle() &&
                    nativeClick.GetButton() == 1)
                {
                    _click.Group(nativeClick);
                    break;
                }
            }
        }

        private void OnUnmap(Gtk.Widget sender, EventArgs args)
        {
            DetachController();
        }

        private void DetachController()
        {
            _pressedItem = null;
            if (_click is not null)
            {
                _click.OnPressed -= OnPressed;
                _click.OnReleased -= OnReleased;
                _click.OnStopped -= OnStopped;
                _click.Ungroup();
                _target?.RemoveController(_click);
            }

            _click = null;
            _target = null;
        }

        private void OnPressed(Gtk.GestureClick sender, Gtk.GestureClick.PressedSignalArgs args)
        {
            _pressedItem = _row.BindingContext;
        }

        private void OnStopped(Gtk.GestureClick sender, EventArgs args)
        {
            _pressedItem = null;
        }

        private void OnBindingContextChanged(object? sender, EventArgs args)
        {
            _pressedItem = null;
            _bindingVersion++;
        }

        private void OnReleased(Gtk.GestureClick sender, Gtk.GestureClick.ReleasedSignalArgs args)
        {
            var item = _pressedItem;
            _pressedItem = null;
            if (_disposed || _target is null || args.NPress <= 0 || item is null ||
                !ReferenceEquals(item, _row.BindingContext) ||
                args.X < 0 || args.Y < 0 || args.X >= _target.GetWidth() || args.Y >= _target.GetHeight())
            {
                return;
            }

            // Report each primary release, including the second click: ReportTap pairs them.
            // Leave GTK's event stack before activation can replace ItemsSource and tear down
            // this native row. Selection processing also finishes before we report the click.
            // The pinned GtkDispatcher.Dispatch runs inline on the UI thread; use an idle source.
            var bindingVersion = _bindingVersion;
            GLib.Functions.IdleAdd(0, () =>
            {
                if (!_disposed && bindingVersion == _bindingVersion)
                {
                    _reportTap(item);
                }

                return false;
            });
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _pressedItem = null;
            _row.BindingContextChanged -= OnBindingContextChanged;
            _widget.OnMap -= OnMap;
            _widget.OnUnmap -= OnUnmap;
            DetachController();
        }
    }
}