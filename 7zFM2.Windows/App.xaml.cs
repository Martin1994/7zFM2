namespace SevenZip.FileManager2.Windows.WinUI;

/// <summary>
/// The WinUI application object for the Windows head. <see cref="MauiProgram"/> supplies the
/// shared app.
/// </summary>
/// <remarks>
/// The extra <c>.WinUI</c> namespace segment is not decoration. The head's root namespace is
/// <c>SevenZip.FileManager2.Windows</c>, so declaring <c>App</c> directly in it would shadow
/// <c>SevenZip.FileManager2.App</c> inside <see cref="MauiProgram"/> - C# resolves a simple
/// name outwards from the innermost namespace, and the two types would collide there. Nesting
/// the WinUI app one level deeper keeps the lookup unambiguous, and matches the namespace the
/// official MAUI templates use for their WinUI <c>App</c> class.
/// </remarks>
public partial class App : MauiWinUIApplication
{
    public App()
    {
        InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
