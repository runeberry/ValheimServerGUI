using Avalonia.Controls;

namespace ValheimServerGUI.App.Controls;

/// <summary>Refresh icon button (WinForms <c>RefreshButton</c>): the restart glyph, flashes a confirm check
/// on click. Action via the bound <see cref="Avalonia.Controls.Button.Command"/>.</summary>
public class RefreshButton : IconButton
{
    public RefreshButton()
    {
        IconName = "Restart_16x";
        ConfirmIconName = "StatusOK_16x";
        if (ToolTip.GetTip(this) is null) ToolTip.SetTip(this, "Refresh");
    }
}
