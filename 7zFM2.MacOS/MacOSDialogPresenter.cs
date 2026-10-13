using Microsoft.Maui.Platforms.MacOS.Platform;
using SevenZip.FileManager2.Platform;

namespace SevenZip.FileManager2.MacOS;

internal sealed class MacOSDialogPresenter : IDialogPresenter
{
    public async Task ShowAsync(Page owner, Page dialog)
    {
        MacOSPage.SetModalPresentationStyle(dialog, MacOSModalPresentationStyle.Sheet);
        MacOSPage.SetModalSheetSizesToContent(dialog, true);

        var dismissed = new TaskCompletionSource();
        dialog.Disappearing += (_, _) => dismissed.TrySetResult();

        await owner.Navigation.PushModalAsync(dialog);

        // A sheet is modal: the caller gets the dialog back when it is gone, not when it opens.
        await dismissed.Task;
    }
}
