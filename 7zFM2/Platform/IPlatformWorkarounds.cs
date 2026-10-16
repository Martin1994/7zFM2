namespace SevenZip.FileManager2.Platform;

/// <summary>
/// The hooks where shared code has to compensate for a defect in whatever backend a head runs on.
/// </summary>
/// <remarks>
/// Every member here is a workaround, and the head that needs it implements it - so "how many
/// workarounds are we carrying, and where" is answered by this interface and its implementations,
/// instead of having to be reconstructed from the pages. Each implementation names the defect, the
/// fingerprint that recognises it and the trigger for deleting it.
/// </remarks>
public interface IPlatformWorkarounds
{
    /// <summary>
    /// Called after the page's content is created, before its native handlers are attached.
    /// </summary>
    /// <remarks>
    /// A head can configure measurement before the first layout rather than repair cached geometry
    /// afterwards. Any handler recreation must preserve the same policy for this page.
    /// </remarks>
    void OnPageCreated(Page page);
}
