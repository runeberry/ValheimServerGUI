using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;

namespace ValheimServerGUI.App.Infrastructure;

/// <summary>Copy-to-clipboard via the visual's <see cref="TopLevel"/> (§11.2 "copy-to-clipboard").</summary>
internal static class ClipboardHelper
{
    public static async Task CopyTextAsync(Visual? source, string? text)
    {
        if (source is null || string.IsNullOrEmpty(text)) return;

        var clipboard = TopLevel.GetTopLevel(source)?.Clipboard;
        if (clipboard is not null)
            await clipboard.SetTextAsync(text);
    }
}
