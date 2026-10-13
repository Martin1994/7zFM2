using SevenZip.FileManager2.Platform;

namespace SevenZip.FileManager2.Windows;

/// <summary>
/// Nothing to do here: the backend turns a pushed modal page into one that blocks the window's
/// content, which is the same thing the other heads ask their platform for.
/// </summary>
internal sealed class WindowsDialogPresenter : IDialogPresenter
{
    public Task ShowAsync(Page owner, Page dialog) => owner.Navigation.PushModalAsync(dialog);
}
