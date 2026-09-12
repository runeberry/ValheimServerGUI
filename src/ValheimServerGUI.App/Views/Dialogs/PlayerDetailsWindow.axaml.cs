using Avalonia.Controls;
using Avalonia.Interactivity;
using ValheimServerGUI.App.Infrastructure;
using ValheimServerGUI.App.ViewModels.Dialogs;

namespace ValheimServerGUI.App.Views.Dialogs;

public partial class PlayerDetailsWindow : Window
{
    private bool _confirmedClose;

    public PlayerDetailsWindow()
    {
        InitializeComponent();
        Closing += OnClosingGuard;
    }

    public PlayerDetailsWindow(PlayerDetailsViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private PlayerDetailsViewModel Vm => (PlayerDetailsViewModel)DataContext!;

    private async void CopyPlayerId(object? sender, RoutedEventArgs e)
        => await ClipboardHelper.CopyTextAsync(this, Vm.PlayerId);

    private async void AddCharacter(object? sender, RoutedEventArgs e)
    {
        var name = await new TextPromptWindow("Add Character", "Character name:", maxLength: 64).ShowDialog<string?>(this);
        if (!string.IsNullOrWhiteSpace(name)) Vm.AddCharacter(name);
    }

    private async void RenameCharacter(object? sender, RoutedEventArgs e)
    {
        if (Vm.SelectedCharacter is not { } current) return;
        var name = await new TextPromptWindow("Rename Character", "Character name:", current, maxLength: 64)
            .ShowDialog<string?>(this);
        if (!string.IsNullOrWhiteSpace(name)) Vm.RenameCharacter(current, name);
    }

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        Vm.Save();
        _confirmedClose = true;
        Close(true);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);

    private async void OnClosingGuard(object? sender, WindowClosingEventArgs e)
    {
        if (_confirmedClose || !Vm.IsDirty) return;

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
