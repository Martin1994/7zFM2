using GLib;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics.CodeAnalysis;
using Uno.UI.Runtime.Skia;

namespace SevenZip.FileManager2.Skia.Gtk;

public class Program
{
    public static void Main(string[] args)
    {
        ExceptionManager.UnhandledException += delegate (UnhandledExceptionArgs expArgs)
        {
            Console.WriteLine("GLIB UNHANDLED EXCEPTION" + expArgs.ExceptionObject.ToString());
            expArgs.ExitApplication = true;
        };

        var host = new GtkHost(() => new AppHead(), args);

        host.Run();
    }
}
