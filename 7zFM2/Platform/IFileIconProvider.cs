using SevenZip.FileManager2.Icons;

namespace SevenZip.FileManager2.Platform;

/// <summary>Implemented by the selected UI backend, not selected by the host OS.</summary>
public interface IFileIconProvider
{
    /// <summary>Includes the distinctions that this backend needs (path or file type).</summary>
    string GetCacheKey(FileIconRequest request);

    /// <summary>
    /// Returns encoded PNG data, or null when no native icon is available. May be called on any
    /// thread; implementations offload filesystem work and dispatch native UI work explicitly.
    /// The caller owns the returned bytes; no native object crosses this boundary.
    /// </summary>
    Task<byte[]?> GetIconAsync(FileIconRequest request, int pixelSize);
}