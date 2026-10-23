using System.Diagnostics.CodeAnalysis;

namespace SevenZip.FileManager2.Caching;

/// <summary>A count-bounded cache with least-recently-used eviction and O(1) operations.</summary>
/// <remarks>
/// Not thread-safe: confine access to one owning thread, or synchronize externally.
/// Reads also promote entries and must follow the same access rule as writes.
/// Only manages retention/order; does not load, expire, cancel or dispose cached values.
/// </remarks>
internal sealed class LruCache<TKey, TValue> where TKey : notnull
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>> _entries = new();
    private readonly LinkedList<KeyValuePair<TKey, TValue>> _recency = new();

    public LruCache(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _capacity = capacity;
    }

    public int Count => _entries.Count;

    /// <summary>Gets an entry and marks it as most recently used, if present.</summary>
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (_entries.TryGetValue(key, out var node))
        {
            Promote(node);
            value = node.Value.Value;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Adds or replaces an entry, marking it as most recently used. If capacity is exceeded,
    /// removes the least recently used entry without disposing or cancelling its value.
    /// </summary>
    public void Set(TKey key, TValue value)
    {
        var item = new KeyValuePair<TKey, TValue>(key, value);
        if (_entries.TryGetValue(key, out var existing))
        {
            existing.Value = item;
            Promote(existing);
            return;
        }

        _entries.Add(key, _recency.AddFirst(item));
        if (_entries.Count > _capacity)
        {
            _entries.Remove(_recency.Last!.Value.Key);
            _recency.RemoveLast();
        }
    }

    /// <summary>Forgets every entry without disposing or cancelling its value.</summary>
    public void Clear()
    {
        _entries.Clear();
        _recency.Clear();
    }

    private void Promote(LinkedListNode<KeyValuePair<TKey, TValue>> node)
    {
        _recency.Remove(node);
        _recency.AddFirst(node);
    }
}