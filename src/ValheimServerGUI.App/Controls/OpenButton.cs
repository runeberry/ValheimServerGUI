using Avalonia.Controls;

namespace ValheimServerGUI.App.Controls;

/// <summary>Open-a-folder icon button (WinForms <c>OpenButton</c>): the open-folder glyph, no confirm flash.
/// Action via the bound <see cref="Avalonia.Controls.Button.Command"/>.</summary>
public class OpenButton : IconButton
{
    public OpenButton()
    {
        IconName = "OpenFolder_16x";
        if (ToolTip.GetTip(this) is null) ToolTip.SetTip(this, "Open folder");
    }
}
