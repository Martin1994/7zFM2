namespace SevenZip.FileManager2.Controls;

/// <summary>
/// Optional native click source for generated data rows. Register in a platform head.
/// Selection and activation policy remain in the shared table, not in this adapter.
/// </summary>
public interface IDataTablePlatformInput
{
    /// <summary>
    /// Attach to a row whose handler is ready. Report each completed primary-button click once,
    /// including both clicks of a double-click; do not consume the native selection gesture.
    /// </summary>
    /// <remarks>
    /// Return a subscription which removes native controllers and cancels queued callbacks when
    /// disposed, or null to keep the default MAUI tap recognizer. While a subscription exists,
    /// the row disables its MAUI tap recognizer to avoid duplicate reports. Report the item from
    /// the completed click, not from attachment time. A native selection update may unmap and
    /// replace the row before its completed click is delivered; unmapping alone must not drop it.
    /// Header resizing, keyboard input and rubber-band selection are outside this interface.
    /// </remarks>
    IDisposable? AttachRow(Layout row, Action<object?> reportTap);
}