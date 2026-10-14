using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.App.Views.Dialogs;
using ValheimServerGUI.Localization;

namespace ValheimServerGUI.App.Views;

/// <summary>
/// The Known Characters table bound to a <see cref="KnownCharactersViewModel"/>. Add/Rename prompt for a name
/// with a <see cref="TextPromptWindow"/> owned by the hosting window.
/// </summary>
public partial class KnownCharactersView : UserControl
{
    private readonly AsyncRelayCommand _addCharacter;
    private readonly AsyncRelayCommand _renameCharacter;
    private KnownCharactersViewModel? _wired;

    public KnownCharactersView()
    {
        InitializeComponent();

        _addCharacter = new AsyncRelayCommand(AddCharacterAsync, () => Vm?.HasPlayer == true);
        _renameCharacter = new AsyncRelayCommand(RenameCharacterAsync, () => Vm?.SelectedCharacter is not null);

        AddCharacterButton.Command = _addCharacter;
        // Rename is shared by the Edit button, the row double-click, and the right-click menu.
        EditCharacterButton.Command = _renameCharacter;
        CharactersListView.RowInvokeCommand = _renameCharacter;
        RenameCharacterMenuItem.Command = _renameCharacter;

        DataContextChanged += (_, _) => Wire();
    }

    private KnownCharactersViewModel? Vm => DataContext as KnownCharactersViewModel;

    // Re-evaluate the code-behind commands' enablement as the VM's player/selection changes.
    private void Wire()
    {
        if (_wired is not null) _wired.PropertyChanged -= OnVmPropertyChanged;
        _wired = Vm;
        if (_wired is not null) _wired.PropertyChanged += OnVmPropertyChanged;
        RefreshCommands();
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(KnownCharactersViewModel.HasPlayer) or nameof(KnownCharactersViewModel.SelectedCharacter))
            RefreshCommands();
    }

    private void RefreshCommands()
    {
        _addCharacter.NotifyCanExecuteChanged();
        _renameCharacter.NotifyCanExecuteChanged();
    }

    private Window? OwnerWindow => TopLevel.GetTopLevel(this) as Window;

    private async Task AddCharacterAsync()
    {
        if (Vm is not { } vm || OwnerWindow is not { } owner) return;
        var name = await new TextPromptWindow(Strings.Prompt_AddCharacter_Title, Strings.Prompt_AddCharacter_Message,
            maxLength: 64).ShowDialog<string?>(owner);
        if (!string.IsNullOrWhiteSpace(name)) vm.AddCharacter(name);
    }

    private async Task RenameCharacterAsync()
    {
        if (Vm is not { } vm || OwnerWindow is not { } owner) return;
        if (vm.SelectedCharacter?.CharacterName is not { } current) return;
        var name = await new TextPromptWindow(Strings.Prompt_EditCharacter_Title, string.Format(Strings.Prompt_EditCharacter_Message, current),
            current, maxLength: 64).ShowDialog<string?>(owner);
        if (!string.IsNullOrWhiteSpace(name)) vm.RenameCharacter(current, name);
    }
}
