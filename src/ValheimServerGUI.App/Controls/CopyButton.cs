using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using ValheimServerGUI.App.Infrastructure;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// A copy-to-clipboard icon button (the WinForms <c>CopyButton</c> equivalent): shows the copy glyph, and on
/// click copies <see cref="Text"/> then flashes a green check (StatusOK) for a couple of seconds via the
/// <see cref="IconButton"/> confirm-flash. While the check shows the button is inert but keeps its colour.
/// </summary>
public class CopyButton : IconButton
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<CopyButton, string?>(nameof(Text));

    public CopyButton()
    {
        IconName = "Copy_16x";
        ConfirmIconName = "StatusOK_16x";
        if (ToolTip.GetTip(this) is null) ToolTip.SetTip(this, "Copy");
    }

    /// <summary>The text copied to the clipboard when the button is clicked.</summary>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    // CopyButton has no Command: clicking copies + flashes, rather than routing through the base Command path.
    protected override async void OnClick() => await ConfirmCopyAsync();

    internal async Task ConfirmCopyAsync()
    {
        if (IsConfirming) return;

        ShowConfirm();  // swap to the check + go inert
        await ClipboardHelper.CopyTextAsync(this, Text);
    }
}
