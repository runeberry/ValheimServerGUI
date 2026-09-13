using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using ValheimServerGUI.App.ViewModels.Dialogs;

namespace ValheimServerGUI.App.Views.Dialogs;

public partial class PlayerDetailsWindow : Window
{
    private bool _confirmedClose;

    public PlayerDetailsWindow()
    {
        InitializeComponent();
        Closing += OnClosingGuard;

        // Add/Rename open a text prompt (owned by this window), so they live in the code-behind as commands
        // the AddRemoveListField invokes; Remove is a plain VM command bound in XAML.
        CharactersField.AddCommand = new AsyncRelayCommand(AddCharacterAsync);
        CharactersField.EditCommand = new AsyncRelayCommand(RenameCharacterAsync);
    }

    public PlayerDetailsWindow(PlayerDetailsViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private PlayerDetailsViewModel Vm => (PlayerDetailsViewModel)DataContext!;

    private async Task AddCharacterAsync()
    {
        var name = await new TextPromptWindow("Add Character", "Character name:", maxLength: 64).ShowDialog<string?>(this);
        if (!string.IsNullOrWhiteSpace(name)) Vm.AddCharacter(name);
    }

    private async Task RenameCharacterAsync()
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
