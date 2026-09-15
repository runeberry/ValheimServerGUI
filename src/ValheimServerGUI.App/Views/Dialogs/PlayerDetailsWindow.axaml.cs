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

        // Add/Rename/Edit-name open a text prompt (owned by this window), so they live in the code-behind as
        // commands the buttons invoke; Remove is a plain VM command bound in XAML.
        AddCharacterButton.Command = new AsyncRelayCommand(AddCharacterAsync);
        EditNameButton.Command = new AsyncRelayCommand(EditNameAsync);

        // Rename is shared by the Edit button, the row double-click, and the right-click menu.
        var renameCharacter = new AsyncRelayCommand(RenameCharacterAsync);
        EditCharacterButton.Command = renameCharacter;
        CharactersListView.RowInvokeCommand = renameCharacter;
        RenameCharacterMenuItem.Command = renameCharacter;
    }

    public PlayerDetailsWindow(PlayerDetailsViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private PlayerDetailsViewModel Vm => (PlayerDetailsViewModel)DataContext!;

    private async Task AddCharacterAsync()
    {
        var name = await new TextPromptWindow("Add Character", "Add a Valheim character name for this player:",
            maxLength: 64).ShowDialog<string?>(this);
        if (!string.IsNullOrWhiteSpace(name)) Vm.AddCharacter(name);
    }

    private async Task RenameCharacterAsync()
    {
        if (Vm.SelectedCharacter is not { } current) return;
        var name = await new TextPromptWindow("Edit Character", $"Edit the name for character '{current}'",
            current, maxLength: 64).ShowDialog<string?>(this);
        if (!string.IsNullOrWhiteSpace(name)) Vm.RenameCharacter(current, name);
    }

    private async Task EditNameAsync()
    {
        var name = await new TextPromptWindow("Edit Player Name",
            "Enter a display name for this player (leave blank to clear):",
            Vm.DisplayName, maxLength: 64).ShowDialog<string?>(this);
        // A null result is Cancel (leave unchanged); a blank result clears the override back to "(unknown)".
        if (name is not null) Vm.DisplayName = name.Trim();
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
