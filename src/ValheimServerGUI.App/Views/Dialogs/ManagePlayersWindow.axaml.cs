using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ValheimServerGUI.App.ViewModels.Dialogs;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.App.Views.Dialogs;

/// <summary>
/// Manage Players: the app-global My Accounts / Friends / Banned lists. Save commits the staged defaults and
/// player records; Cancel (or closing without saving) discards them, guarded by the shared unsaved-changes prompt.
/// </summary>
public partial class ManagePlayersWindow : DialogWindow
{
    private readonly IPlayerDataRepository? _repo;
    private readonly IRuneberryApiClient? _api;
    private bool _confirmedClose;

    // Parameterless ctor for the Avalonia runtime loader / designer; the real entry point is the overload below.
    public ManagePlayersWindow()
    {
        InitializeComponent();
        Closing += OnClosingGuard;
    }

    public ManagePlayersWindow(ManagePlayersViewModel viewModel, IPlayerDataRepository repo, IRuneberryApiClient? api)
        : this()
    {
        _repo = repo;
        _api = api;
        DataContext = viewModel;

        viewModel.AddPlayerPrompt = options => new AddPlayerWindow(options).ShowDialog<AddPlayerResult?>(this);
        viewModel.MessagePrompt = (title, body) => MessageBox.ShowAsync(this, title, body);
        viewModel.ChoicePrompt = (title, body) => MessageBox.ConfirmAsync(this, title, body);
        viewModel.DetailsRequested += key => _ = ShowDetailsAsync(key);
        Closed += (_, _) => viewModel.Dispose();
    }

    private ManagePlayersViewModel Vm => (ManagePlayersViewModel)DataContext!;

    // Player Details edits the dialog's staged records, so its OK is only kept if this dialog is saved.
    private async Task ShowDetailsAsync(string key)
    {
        if (_repo is null) return;
        var saved = await new PlayerDetailsWindow(new PlayerDetailsViewModel(_repo, key, _api, Vm.Records))
            .ShowDialog<bool>(this);
        if (saved) Vm.OnDetailsSaved(key);
    }

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
