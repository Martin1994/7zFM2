using AppKit;
using Foundation;
using Microsoft.Maui.Platforms.MacOS.Platform;

namespace SevenZip.FileManager2.MacOS;

public class MainClass
{
    private static void Main(string[] args)
    {
        NSApplication.Init();
        NSApplication.SharedApplication.Delegate = new MacOSAppDelegate();
        NSApplication.Main(args);
    }
}

// NOTE: the class name must be a legal C# identifier. The maui-macos template substitutes the
// project name verbatim here, which does not compile for any project name containing dots.
[Register("MacOSAppDelegate")]
public class MacOSAppDelegate : MacOSMauiApplication
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
