using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ValheimServerGUI.Game;

namespace ValheimServerGUI.App.ViewModels.Dialogs;

/// <summary>
/// One Manage Players tab (My Accounts / Friends / Banned): the accounts on that list with their default role,
/// the selected account's Known Characters (not on Banned), and the list's commands. All state changes route
/// through the owning <see cref="ManagePlayersViewModel"/>, which holds the staged defaults and dirty flag.
/// </summary>
public partial class PlayerListSectionViewModel : ObservableObject
{
    private readonly ManagePlayersViewModel _owner;

    internal PlayerListSectionViewModel(
        ManagePlayersViewModel owner, PlayerCategory category, string? caption, string header,
        KnownCharactersViewModel? knownCharacters)
    {
        _owner = owner;
        Category = category;
        Caption = caption;
        Header = header;
        KnownCharacters = knownCharacters;
    }

    public PlayerCategory Category { get; }

    /// <summary>Explanatory text above the table (null = none).</summary>
    public string? Caption { get; }

    /// <summary>The table's header label.</summary>
    public string Header { get; }

    public ObservableCollection<PlayerRowViewModel> Accounts { get; } = new();

    /// <summary>The selected account's known characters; null on the Banned list.</summary>
    public KnownCharactersViewModel? KnownCharacters { get; }

    public bool HasKnownCharacters => KnownCharacters is not null;

    /// <summary>Default-role verbs apply to My Accounts and Friends; a Banned entry is always Banned.</summary>
    public bool HasRoleCommands => Category != PlayerCategory.Banned;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AdminToggleLabel), nameof(PermitToggleLabel))]
    [NotifyCanExecuteChangedFor(nameof(RemoveCommand), nameof(ViewDetailsCommand),
        nameof(ToggleAdminCommand), nameof(TogglePermitCommand))]
    private PlayerRowViewModel? _selectedAccount;

    // Labels follow the selected account's default role (derived, never mirrored).
    public string AdminToggleLabel => SelectedAccount?.DisplayRole == PlayerRole.Admin ? "Remove admin" : "Make admin";
    public string PermitToggleLabel =>
        SelectedAccount?.DisplayRole == PlayerRole.Permitted ? "Revoke join permission" : "Add to permitted";

    private bool HasSelection => SelectedAccount is not null;
    private bool CanEditRole => HasSelection && HasRoleCommands;

    partial void OnSelectedAccountChanged(PlayerRowViewModel? value) => _owner.OnSelectionChanged(this);

    /// <summary>Re-raises the role-derived labels after the selected row's role changed in place.</summary>
    internal void RefreshRoleLabels()
    {
        OnPropertyChanged(nameof(AdminToggleLabel));
        OnPropertyChanged(nameof(PermitToggleLabel));
    }

    [RelayCommand]
    private Task Add() => _owner.AddAsync(this);

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Remove()
    {
        if (SelectedAccount is { } row) _owner.RemoveAccount(this, row.Key);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void ViewDetails()
    {
        if (SelectedAccount is { } row) _owner.RequestDetails(row.Key);
    }

    [RelayCommand(CanExecute = nameof(CanEditRole))]
    private void ToggleAdmin() => ToggleRole(PlayerRole.Admin);

    [RelayCommand(CanExecute = nameof(CanEditRole))]
    private void TogglePermit() => ToggleRole(PlayerRole.Permitted);

    // Positive verb sets the default role; the negative verb drops it to None (the account stays on the list).
    private void ToggleRole(PlayerRole role)
    {
        if (SelectedAccount is not { } row) return;
        _owner.SetDefaultRole(row.Key, row.DisplayRole == role ? PlayerRole.None : role);
    }
}
