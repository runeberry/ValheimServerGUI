using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace ValheimServerGUI.App.Views.Dialogs;

/// <summary>
/// Shared base for the app's modal dialogs. Its one job is reliable owner-centering: Avalonia's
/// <see cref="WindowStartupLocation.CenterOwner"/> computes the position before a
/// <c>SizeToContent="WidthAndHeight"</c> window has been measured, so the dialog lands offset by half its own
/// size. Every dialog sizes to content, so they all re-center here once the real size is known.
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
        if (Owner is not Window owner) return;

        // Positions are in physical pixels; sizes are in DIPs — convert with the (shared-screen) owner scaling.
        var scaling = owner.RenderScaling;
        var ownerSize = (owner.FrameSize ?? owner.ClientSize) * scaling;
        var mySize = (FrameSize ?? ClientSize) * scaling;
        var ownerPos = owner.Position;

        Position = new PixelPoint(
            ownerPos.X + (int)((ownerSize.Width - mySize.Width) / 2),
            ownerPos.Y + (int)((ownerSize.Height - mySize.Height) / 2));
    }
}
