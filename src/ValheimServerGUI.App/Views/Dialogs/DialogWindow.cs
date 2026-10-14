using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using ValheimServerGUI.App.Infrastructure;

namespace ValheimServerGUI.App.Views.Dialogs;

/// <summary>
/// Shared base for the app's modal dialogs. Its one job is reliable owner-centering: Avalonia's
/// <see cref="WindowStartupLocation.CenterOwner"/> computes the position before a
/// <c>SizeToContent="WidthAndHeight"</c> window has been measured, so the dialog lands offset by half its own
/// size. Every dialog sizes to content, so they all re-center here once the real size is known. A dialog shown
/// without an owner centers over the app's active window instead of the screen, so it never opens on a monitor
/// away from the app. The centred position is then kept inside the owner screen's working area, so a dialog
/// taller than its owner can't open with its title bar off-screen.
/// </summary>
public class DialogWindow : Window
{
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // If the size isn't measured yet (some backends only settle it after Opened), defer one tick so the
        // computed centre uses the final size rather than 0×0.
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
            Dispatcher.UIThread.Post(CenterOverOwner, DispatcherPriority.Loaded);
        else
            CenterOverOwner();
    }

    private void CenterOverOwner()
    {
        if (WindowStartupLocation != WindowStartupLocation.CenterOwner) return;
        if ((Owner as Window ?? WindowLocator.AnchorFor(this)) is not { } owner) return;

        // Positions are in physical pixels; sizes are in DIPs — convert with the (shared-screen) owner scaling.
        var scaling = owner.RenderScaling;
        var ownerFrame = new PixelRect(owner.Position, PixelSize.FromSize(owner.FrameSize ?? owner.ClientSize, scaling));
        var mySize = PixelSize.FromSize(FrameSize ?? ClientSize, scaling);
        var workingArea = owner.Screens.ScreenFromWindow(owner)?.WorkingArea;

        Position = PlaceOverOwner(ownerFrame, mySize, workingArea);
    }

    /// <summary>
    /// Centres a dialog of <paramref name="dialogSize"/> over <paramref name="ownerFrame"/>, then shifts it to lie
    /// inside <paramref name="workingArea"/> when one is known. A dialog larger than the working area is pinned to
    /// its top-left so the title bar stays reachable. All values are physical pixels.
    /// </summary>
    internal static PixelPoint PlaceOverOwner(PixelRect ownerFrame, PixelSize dialogSize, PixelRect? workingArea)
    {
        var x = ownerFrame.X + (ownerFrame.Width - dialogSize.Width) / 2;
        var y = ownerFrame.Y + (ownerFrame.Height - dialogSize.Height) / 2;

        if (workingArea is { } area)
        {
            x = Math.Max(area.X, Math.Min(x, area.Right - dialogSize.Width));
            y = Math.Max(area.Y, Math.Min(y, area.Bottom - dialogSize.Height));
        }

        return new PixelPoint(x, y);
    }
}
