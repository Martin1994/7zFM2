namespace SevenZip.FileManager2;

/// <summary>
/// The application's <see cref="Shell"/>, and the root of every window.
/// </summary>
/// <remarks>
/// Standard MAUI app structure - the template's <c>CreateWindow</c> is
/// <c>new Window(new AppShell())</c> - because Shell is the layer that describes an app's visual
/// hierarchy and navigation, and gives it URI-based routing and a search handler.
/// It also carries the menu bar: a <see cref="ContentPage"/> only displays its
/// <see cref="ContentPage.MenuBarItems"/> when it is hosted in a Shell app (or a
/// <see cref="NavigationPage"/>).
/// </remarks>
public sealed class AppShell : Shell
{
    public AppShell()
    {
        // The app has a single page, so there is nothing to put in a flyout.
        FlyoutBehavior = FlyoutBehavior.Disabled;

        Items.Add(new ShellContent
        {
            ContentTemplate = new DataTemplate(() => new FileManagerPage()),
        });
    }
}
