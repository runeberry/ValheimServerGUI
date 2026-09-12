using System.Threading.Tasks;
using Avalonia.Controls;

namespace ValheimServerGUI.App.Views.Dialogs;

/// <summary>Shared unsaved-changes guards for the edit dialogs (§13.3).</summary>
internal static class DialogGuards
{
    /// <summary>Yes/No "discard unsaved changes?" — returns true to discard and close.</summary>
    public static async Task<bool> ConfirmDiscardAsync(Window owner)
        => await new ConfirmWindow("Unsaved changes",
            "You have unsaved changes. Discard them?").ShowDialog<bool>(owner);

    /// <summary>Save / Don't Save / Cancel on close (§13.3).</summary>
    public static async Task<UnsavedChangesChoice> ConfirmSaveDiscardCancelAsync(Window owner)
        => await new UnsavedChangesWindow().ShowDialog<UnsavedChangesChoice>(owner);
}
