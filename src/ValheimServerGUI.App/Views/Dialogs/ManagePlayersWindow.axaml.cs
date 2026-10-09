using Avalonia.Controls;
using Avalonia.Interactivity;
using ValheimServerGUI.App.ViewModels.Dialogs;
using ValheimServerGUI.Localization;

namespace ValheimServerGUI.App.Views.Dialogs;

/// <summary>
/// Manage Players: every known player's app-global default role, on a Player Accounts tab and a Banned tab, with
/// each account's name and Known Characters. Save commits the staged defaults and player records; Cancel (or
/// closing without saving) discards them, guarded by the shared unsaved-changes prompt.
/// </summary>
public partial class ManagePlayersWindow : DialogWindow
{
    private bool _confirmedClose;

    // Parameterless ctor for the Avalonia runtime loader / designer; the real entry point is the overload below.
    public ManagePlayersWindow()
    {
        InitializeComponent();
        Closing += OnClosingGuard;
    }

    public ManagePlayersWindow(ManagePlayersViewModel viewModel) : this()
    {
        DataContext = viewModel;

        viewModel.AddPlayerPrompt = options => new AddPlayerWindow(options).ShowDialog<AddPlayerResult?>(this);
        viewModel.EditNamePrompt = current => new TextPromptWindow(Strings.Prompt_EditPlayerName_Title,
            Strings.Prompt_EditPlayerName_Message, current, maxLength: 64).ShowDialog<string?>(this);
        Closed += (_, _) => viewModel.Dispose();
    }

    private ManagePlayersViewModel Vm => (ManagePlayersViewModel)DataContext!;

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        Vm.Save();
        _confirmedClose = true;
        Close(true);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);

    private async void OnClosingGuard(object? sender, WindowClosingEventArgs e)
    {
        if (_confirmedClose || DataContext is not ManagePlayersViewModel { IsDirty: true }) return;

        e.Cancel = true;
        switch (await DialogGuards.ConfirmSaveDiscardCancelAsync(this))
        {
            case UnsavedChangesChoice.Save:
                Vm.Save();
                _confirmedClose = true;
                Close(true);
                break;
            case UnsavedChangesChoice.Discard:
                _confirmedClose = true;
                Close(false);
                break;
        }
    }
}
