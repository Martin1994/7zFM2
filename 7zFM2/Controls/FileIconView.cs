using System.Diagnostics;
using CommunityToolkit.Maui;
using SevenZip.FileManager2.Icons;

namespace SevenZip.FileManager2.Controls;

/// <summary>UI adapter: starts lookups only for realized cells and rejects recycled-row results.</summary>
// Future: AppKit NSImageView / GTK Gtk.Image handlers could avoid PNG encoding/decoding,
// preserve native image representations, and let GTK handle symbolic colors/theme changes.
public sealed partial class FileIconView : Image
{
    private FileIconCache? _cache;
    private EventHandler? _cacheChanged;
    private bool _active;
    private int _version;

    [BindableProperty(PropertyChangedMethodName = nameof(OnIconChanged))]
    public partial FileIconRequest? Icon { get; set; }

    private static void OnIconChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((FileIconView)bindable).Refresh();

    public FileIconView()
    {
        WidthRequest = HeightRequest = 16;
        VerticalOptions = LayoutOptions.Center;
        Aspect = Aspect.AspectFit;
        InputTransparent = true;
        Loaded += (_, _) => Activate();
        Unloaded += (_, _) => Deactivate();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is null)
            Deactivate();
        else
            Activate();
    }

    private void Activate()
    {
        Deactivate();
        _cache = Handler?.MauiContext?.Services.GetRequiredService<FileIconCache>();
        _active = _cache is not null;
        if (_cache is { } cache)
        {
            // The GTK CollectionView creates rows without a logical Parent: even mapped
            // cells never receive Loaded/Unloaded. Handler creation is sufficient to load
            // their icons, and a weak subscription prevents the cache from retaining them.
            var reference = new WeakReference<FileIconView>(this);
            EventHandler? changed = null;
            changed = (_, _) =>
            {
                if (reference.TryGetTarget(out var view))
                    view.OnIconsChanged(cache, EventArgs.Empty);
                else
                    cache.Changed -= changed;
            };
            _cacheChanged = changed;
            cache.Changed += changed;
        }
        Refresh();
    }

    private void Deactivate()
    {
        _active = false;
        ++_version;
        if (_cache is not null)
            _cache.Changed -= _cacheChanged;
        _cacheChanged = null;
        _cache = null;
        Source = null;
    }

    private void OnIconsChanged(object? sender, EventArgs args) => Refresh();

    // Void callback boundary: observe both synchronous and awaited refresh failures here.
    private async void Refresh()
    {
        var version = ++_version;
        ILogger<FileIconView>? logger = null;
        try
        {
            logger = Handler?.MauiContext?.Services.GetService<ILogger<FileIconView>>();
            // Current callers are UI callbacks, but GTK handler setup can run before
            // ownership of the GLib context. Dispatch only when the backend requires it.
            var dispatcher = Dispatcher;
            if (dispatcher.IsDispatchRequired)
                await dispatcher.DispatchAsync(() => RefreshAsync(version)).ConfigureAwait(false);
            else
                await RefreshAsync(version).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            if (logger is not null)
                logger.LogError(exception, "Could not refresh file icon.");
            else
                Console.Error.WriteLine($"Could not refresh file icon: {exception}");
        }
    }

    private async Task RefreshAsync(int version)
    {
        Debug.Assert(!Dispatcher.IsDispatchRequired);
        if (version != _version)
            return;

        Source = Icon is { } icon ? DefaultFileIconSources.ForKind(icon.Kind) : null;

        if (_active && _cache is not null && Icon is { } request)
        {
            double density = 1;
            try { density = DeviceDisplay.Current.MainDisplayInfo.Density; }
            catch (NotImplementedException) { }
            if (!double.IsFinite(density) || density <= 0)
                density = 1;
            // Overlapping refreshes are allowed; reject stale results rather than cancel shared loads.
            await LoadAsync(_cache, request, (int)Math.Ceiling(16 * density), version).ConfigureAwait(false);
        }
    }

    private async Task LoadAsync(FileIconCache cache, FileIconRequest request, int pixels, int version)
    {
        var dispatcher = Dispatcher;
        var png = await cache.GetAsync(request, pixels).ConfigureAwait(false);
        if (dispatcher.IsDispatchRequired)
            await dispatcher.DispatchAsync(UpdateSource).ConfigureAwait(false);
        else
            UpdateSource();

        void UpdateSource()
        {
            Debug.Assert(!dispatcher.IsDispatchRequired);
            if (!_active || version != _version || png is null)
                return;

            // MAUI may open the image more than once. Never return a reused/disposed stream.
            Source = ImageSource.FromStream(() => new MemoryStream(png, writable: false));
        }
    }
}