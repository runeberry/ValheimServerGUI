using System.Threading.Tasks;
using Avalonia.Controls;
using ValheimServerGUI.Localization;

namespace ValheimServerGUI.App.Views.Dialogs;

/// <summary>Shared unsaved-changes guards for the edit dialogs (§13.3).</summary>
internal static class DialogGuards
{
    /// <summary>Yes/No "discard unsaved changes?" — returns true to discard and close.</summary>
    public static Task<bool> ConfirmDiscardAsync(Window owner)
        => MessageBox.ConfirmAsync(owner, Strings.Prompt_UnsavedChanges_Title, Strings.Prompt_UnsavedChanges_DiscardMessage);

    /// <summary>Save Changes / Discard Changes / Cancel on close (§13.3).</summary>
    public static Task<UnsavedChangesChoice> ConfirmSaveDiscardCancelAsync(Window owner)
        => MessageBox.ChooseAsync<UnsavedChangesChoice>(owner, Strings.Prompt_UnsavedChanges_Title,
            Strings.Prompt_UnsavedChanges_SaveMessage,
            new MessageBoxButton(Strings.Prompt_UnsavedChanges_Save, UnsavedChangesChoice.Save, isDefault: true),
            new MessageBoxButton(Strings.Prompt_UnsavedChanges_Discard, UnsavedChangesChoice.Discard),
            new MessageBoxButton(Strings.Common_Cancel, UnsavedChangesChoice.Cancel, isCancel: true));
}
