namespace SevenZip.FileManager2.Platform;

/// <summary>
/// Presents <paramref name="dialog"/> over <paramref name="owner"/>, and returns when the user has
/// dismissed it.
/// </summary>
/// <remarks>
/// Presenting is per platform - an AppKit sheet on macOS, a dialog window on GTK4, MAUI's own modal
/// page (and the backend's focus trap) on Windows - so the head supplies it. The dialog is a page
/// either way, which is what makes the one interface enough.
/// </remarks>
public interface IDialogPresenter
{
    Task ShowAsync(Page owner, Page dialog);
}
