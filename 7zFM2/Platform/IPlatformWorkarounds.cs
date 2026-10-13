namespace SevenZip.FileManager2.Platform;

/// <summary>
/// The hooks where shared code has to compensate for a defect in whatever backend a head runs on.
/// </summary>
/// <remarks>
/// Every member here is a workaround, and the head that needs it implements it - so "how many
/// workarounds are we carrying, and where" is answered by this interface and its implementations,
/// instead of having to be reconstructed from the pages. Each implementation names the defect, the
/// fingerprint that recognises it and the trigger for deleting it, and points at the measurements in
/// <c>docs/upstream-defects.md</c>.
/// </remarks>
public interface IPlatformWorkarounds
{
    /// <summary>
    /// Called after the page has been laid out, once per layout, including the ones nobody asked for.
    /// </summary>
    /// <remarks>
    /// An implementation that only cares about the first, settled layout has to work out where that is;
    /// only the backend knows. Calls are frequent, so an implementation must be cheap when it has
    /// nothing to do.
    /// </remarks>
    void OnPageLayout(Page page);
}
