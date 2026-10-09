using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using ValheimServerGUI.App.ViewModels.Dialogs;
using ValheimServerGUI.Localization;

namespace ValheimServerGUI.App.Views.Dialogs;

public partial class PlayerDetailsWindow : DialogWindow
{
    private bool _confirmedClose;

    public PlayerDetailsWindow()
    {
        InitializeComponent();
        Closing += OnClosingGuard;

        // Edit-name opens a text prompt owned by this window, so it lives in the code-behind. The Known
        // Characters table (KnownCharactersView) wires its own add/rename prompts.
        EditNameButton.Command = new AsyncRelayCommand(EditNameAsync);
    }

    public PlayerDetailsWindow(PlayerDetailsViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private PlayerDetailsViewModel Vm => (PlayerDetailsViewModel)DataContext!;

    private async Task EditNameAsync()
    {
        var name = await new TextPromptWindow(Strings.Prompt_EditPlayerName_Title,
            Strings.Prompt_EditPlayerName_Message,
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
