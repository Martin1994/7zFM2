using AppKit;
using CoreGraphics;
using Microsoft.Maui.Platforms.MacOS.Handlers;
using Microsoft.Maui.Platforms.MacOS.Platform;

namespace SevenZip.FileManager2.MacOS;

/// <summary>
/// Adds a native footer material to the status row, leaving MAUI measurement and layout intact.
/// </summary>
internal sealed class MacOSStatusBarHandler : LayoutHandler
{
    private NSVisualEffectView? _background;

    protected override void ConnectHandler(MacOSContainerView platformView)
    {
        base.ConnectHandler(platformView);

        // This is a native decoration, not a MAUI layout child: it fills the padding as well
        // as the content, without contributing an intrinsic size to the status row.
        _background = new StatusBarEffectView
        {
            Frame = platformView.Bounds,
            AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.HeightSizable,
            Material = NSVisualEffectMaterial.HeaderView,
            BlendingMode = NSVisualEffectBlendingMode.BehindWindow,
            State = NSVisualEffectState.FollowsWindowActiveState,
            AccessibilityElement = false,
        };
        platformView.AddSubview(_background, NSWindowOrderingMode.Below, null);
    }

    protected override void DisconnectHandler(MacOSContainerView platformView)
    {
        // Shell navigation can recreate the page's handlers. Remove the decoration from the
        // old native view; the replacement handler will create its own material.
        if (_background is not null)
        {
            _background.RemoveFromSuperview();
            _background.Dispose();
            _background = null;
        }

        base.DisconnectHandler(platformView);
    }

    private sealed class StatusBarEffectView : NSVisualEffectView
    {
        public override NSView? HitTest(CGPoint point)
        {
            // A decorative background must not become an input target.
            return null;
        }
    }
}