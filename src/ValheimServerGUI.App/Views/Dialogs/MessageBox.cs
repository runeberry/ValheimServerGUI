using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace ValheimServerGUI.App.Views.Dialogs;

/// <summary>
/// Async facade over <see cref="MessageBoxWindow"/> — the app's WinForms <c>MessageBox.Show</c> replacement.
/// All modal prompts (info, yes/no, multi-choice) route through here so there is one dialog implementation and
/// no per-prompt window classes.
/// </summary>
public static class MessageBox
{
    /// <summary>Shows an informational message with a single OK button.</summary>
    public static Task ShowAsync(Window owner, string title, string message, string okCaption = "OK")
        => new MessageBoxWindow(title, message,
            new[] { new MessageBoxButton(okCaption, isDefault: true, isCancel: true) })
            .ShowDialog(owner);

    /// <summary>Shows a two-button confirmation; returns true for the confirm button, false for cancel/close.</summary>
    public static Task<bool> ConfirmAsync(
        Window owner, string title, string message, string confirmCaption = "Yes", string cancelCaption = "No")
        => ChooseAsync<bool>(owner, title, message,
            new MessageBoxButton(confirmCaption, true, isDefault: true),
            new MessageBoxButton(cancelCaption, false, isCancel: true));

    /// <summary>
    /// Shows a set of buttons and returns the chosen button's typed result. Closing via the window chrome
    /// (with no button) yields the cancel button's result, or <c>default</c> when none is marked cancel.
    /// </summary>
    public static async Task<T> ChooseAsync<T>(
        Window owner, string title, string message, params MessageBoxButton[] buttons)
    {
        var result = await new MessageBoxWindow(title, message, buttons).ShowDialog<object?>(owner);
        if (result is T chosen) return chosen;

        var cancel = buttons.FirstOrDefault(b => b.IsCancel);
        return cancel is { Result: T cancelResult } ? cancelResult : default!;
    }
}
