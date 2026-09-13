using Avalonia.Controls;

namespace ValheimServerGUI.App.Controls;

/// <summary>Settings icon button (WinForms <c>SettingsButton</c>): the settings glyph, no confirm flash.
/// Action via the bound <see cref="Avalonia.Controls.Button.Command"/>.</summary>
public class SettingsButton : IconButton
{
    public SettingsButton()
    {
        IconName = "Settings_16x";
        if (ToolTip.GetTip(this) is null) ToolTip.SetTip(this, "Settings");
    }
}
