using System.Threading.Tasks;
using Avalonia.Controls;

namespace ValheimServerGUI.App.Views.Dialogs;

/// <summary>Shared unsaved-changes guards for the edit dialogs (§13.3).</summary>
internal static class DialogGuards
{
    /// <summary>Yes/No "discard unsaved changes?" — returns true to discard and close.</summary>
    public static Task<bool> ConfirmDiscardAsync(Window owner)
        => MessageBox.ConfirmAsync(owner, "Unsaved changes", "You have unsaved changes. Discard them?");

    /// <summary>Save / Don't Save / Cancel on close (§13.3).</summary>
    public static Task<UnsavedChangesChoice> ConfirmSaveDiscardCancelAsync(Window owner)
        => MessageBox.ChooseAsync<UnsavedChangesChoice>(owner, "Unsaved changes",
            "You have unsaved changes. Save them before closing?",
            new MessageBoxButton("Save", UnsavedChangesChoice.Save, isDefault: true),
            new MessageBoxButton("Don't Save", UnsavedChangesChoice.Discard),
            new MessageBoxButton("Cancel", UnsavedChangesChoice.Cancel, isCancel: true));
}
