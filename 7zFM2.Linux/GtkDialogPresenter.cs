using Microsoft.Maui.Platforms.Linux.Gtk4.Platform;
using SevenZip.FileManager2.Platform;

namespace SevenZip.FileManager2.Linux;

internal sealed class GtkDialogPresenter : IDialogPresenter
{
    public Task ShowAsync(Page owner, Page dialog)
    {
        GtkPage.SetModalPresentationStyle(dialog, GtkModalPresentationStyle.Dialog);
        GtkPage.SetModalSizesToContent(dialog, true);

        return owner.Navigation.PushModalAsync(dialog);
    }
}
