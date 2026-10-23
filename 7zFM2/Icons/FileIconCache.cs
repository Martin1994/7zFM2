using System.Diagnostics;
using Microsoft.Maui.Dispatching;
using SevenZip.FileManager2.Caching;
using SevenZip.FileManager2.Platform;

namespace SevenZip.FileManager2.Icons;

/// <summary>Session-only icon cache whose state belongs exclusively to its UI dispatcher.</summary>
public sealed class FileIconCache(IFileIconProvider provider, IDispatcher dispatcher)
{
    private const int Capacity = 256;
    // A conservative policy for bursts of distinct cache misses, not a native API requirement.
    // Bounds simultaneous shell/GIO lookups and image conversion; tune with profiling if needed.
    private const int MaxConcurrentLoads = 4;

    // Only the owning UI thread accesses the LRU; workers return immutable load results.
    private readonly LruCache<CacheKey, Entry> _entries = new(Capacity);
    private readonly SemaphoreSlim _workers = new(MaxConcurrentLoads);

    /// <summary>Raised on the owning UI thread after <see cref="Clear"/>.</summary>
    /// <remarks>Subscribe and unsubscribe on that same thread.</remarks>
    public event EventHandler? Changed;

    /// <summary>Gets a cached file icon, or starts/joins an asynchronous native icon lookup.</summary>
    /// <param name="request">
    /// The icon description. The provider determines which descriptions identify the same icon.
    /// </param>
    /// <param name="pixelSize">
    /// Requested size in physical pixels, clamped to 16–64 before lookup and key generation.
    /// The encoded image's actual dimensions depend on the provider.
    /// </param>
    /// <returns>
    /// A task yielding shared, cache-owned PNG bytes, or <see langword="null"/> when the provider
    /// returns no icon or its icon-loading operation throws. Callers must not modify the array.
    /// The returned task may already be complete.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Must be called on the owning UI thread. Requests with the same provider key and clamped
    /// pixel size share a task while the entry remains cached and unexpired; awaiting that task
    /// is safe on any thread. Exceptions from the provider's
    /// key-generation method propagate synchronously rather than being converted to a cache miss.
    /// </para>
    /// <para>
    /// Successful results expire five minutes after loading; null results expire after 15 seconds.
    /// Expiry is checked on lookup. Capacity eviction or <see cref="Clear"/> can remove entries
    /// sooner, but does not cancel their pending work or invalidate bytes already returned.
    /// Subsequent requests for removed entries may therefore start another load.
    /// </para>
    /// <para>
    /// The loading task is stored directly; providers must not synchronously reenter this cache
    /// while creating it. Loading does not rely on an ambient SynchronizationContext.
    /// Data and expiry are returned together as an immutable loading result; loading never
    /// writes back to a cache entry or dispatches expiry updates. Pending tasks are reused;
    /// expiry is read only from successfully completed results, without blocking the UI thread.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Called outside the owning UI thread.</exception>
    public Task<byte[]?> GetAsync(FileIconRequest request, int pixelSize)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(request);
        pixelSize = Math.Clamp(pixelSize, 16, 64);
        var key = new CacheKey(provider.GetCacheKey(request), pixelSize);
        if (_entries.TryGetValue(key, out var existing)
            && (!existing.LoadTask.IsCompleted || (existing.LoadTask.IsCompletedSuccessfully
                && existing.LoadTask.Result.Expires > DateTimeOffset.UtcNow)))
        {
            return existing.DataTask;
        }

        var entry = new Entry(LoadAsync(request, pixelSize));
        _entries.Set(key, entry);
        return entry.DataTask;
    }

    /// <summary>Clears retained entries and raises <see cref="Changed"/> on the owning UI thread.</summary>
    /// <remarks>Pending loads continue; their completion never reinserts an evicted entry.</remarks>
    /// <exception cref="InvalidOperationException">Called outside the owning UI thread.</exception>
    public void Clear()
    {
        VerifyAccess();
        _entries.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void VerifyAccess()
    {
        if (dispatcher.IsDispatchRequired)
            throw new InvalidOperationException("FileIconCache must be accessed on its owning UI thread.");
    }

    private async Task<IconLoadResult> LoadAsync(FileIconRequest request, int pixelSize)
    {
        await _workers.WaitAsync().ConfigureAwait(false);
        byte[]? data = null;
        try
        {
            data = await provider.GetIconAsync(request, pixelSize).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            // An unavailable shell/theme must not prevent browsing. Retry failures soon.
            Debug.WriteLine($"File icon lookup failed: {exception.Message}");
        }
        finally
        {
            _workers.Release();
        }

        return new IconLoadResult(data, DateTimeOffset.UtcNow.AddSeconds(data is null ? 15 : 300));
    }

    private readonly record struct CacheKey(string ProviderKey, int PixelSize);

    private readonly record struct IconLoadResult(byte[]? Data, DateTimeOffset Expires);

    private sealed class Entry(Task<IconLoadResult> loadTask)
    {
        public Task<IconLoadResult> LoadTask { get; } = loadTask;
        // Create the projection once per entry so public callers also share a single Task.
        public Task<byte[]?> DataTask { get; } = GetDataAsync(loadTask);

        private static async Task<byte[]?> GetDataAsync(Task<IconLoadResult> task) =>
            (await task.ConfigureAwait(false)).Data;
    }
}