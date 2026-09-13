using Avalonia.Controls;

namespace ValheimServerGUI.App.Controls;

/// <summary>Edit icon button (WinForms <c>EditButton</c>): the edit glyph, flashes a confirm check on click.
/// Action via the bound <see cref="Avalonia.Controls.Button.Command"/>.</summary>
public class EditButton : IconButton
{
    public EditButton()
    {
        IconName = "Edit_16x";
        ConfirmIconName = "StatusOK_16x";
        if (ToolTip.GetTip(this) is null) ToolTip.SetTip(this, "Edit");
    }
}
