using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ValheimServerGUI.Game;
using ValheimServerGUI.Localization;

namespace ValheimServerGUI.App.ViewModels.Dialogs;

/// <summary>
/// One Manage Players tab: <b>Player Accounts</b> (every known player whose default role is not Banned) or
/// <b>Banned</b> (default role Banned). Holds the rows, the selection, the selected account's Known Characters
/// (Player Accounts only), and the row commands. All state changes route through the owning
/// <see cref="ManagePlayersViewModel"/>, which holds the staged data and the dirty flag.
/// </summary>
public partial class PlayerListSectionViewModel : ObservableObject
{
    private readonly ManagePlayersViewModel _owner;

    internal PlayerListSectionViewModel(ManagePlayersViewModel owner, bool isBanned, KnownCharactersViewModel? knownCharacters)
    {
        _owner = owner;
        IsBanned = isBanned;
        KnownCharacters = knownCharacters;
        Accounts.CollectionChanged += (_, _) => OnPropertyChanged(nameof(AccountsEmptyText));
    }

    /// <summary>True for the Banned tab (rows whose default role is Banned).</summary>
    public bool IsBanned { get; }

    public ObservableCollection<PlayerRowViewModel> Accounts { get; } = new();

    /// <summary>The accounts table's empty-state hint, or null when it has rows.</summary>
    public string? AccountsEmptyText => Accounts.Count == 0 ? Strings.ManagePlayers_NoAccounts : null;

    /// <summary>The selected account's known characters; null on the Banned tab.</summary>
    public KnownCharactersViewModel? KnownCharacters { get; }

    public bool HasKnownCharacters => KnownCharacters is not null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DefaultIsAdmin), nameof(DefaultIsPermitted), nameof(DefaultIsBanned),
        nameof(DefaultIsNone), nameof(HasSelection))]
    [NotifyCanExecuteChangedFor(nameof(RemoveCommand), nameof(ViewDetailsCommand))]
    private PlayerRowViewModel? _selectedAccount;

    public bool HasSelection => SelectedAccount is not null;

    /// <summary>Forgetting a player needs them Offline (same rule as the Players tab).</summary>
    private bool CanRemove => SelectedAccount is { IsOffline: true };

    // "Set default role" radio items: each reads the selected row's default role and, when checked, sets it.
    // A radio group's uncheck-the-others `false` writes are ignored; the owner re-derives these after a change.
    public bool DefaultIsAdmin { get => Is(PlayerRole.Admin); set => SetIfChecked(value, PlayerRole.Admin); }
    public bool DefaultIsPermitted { get => Is(PlayerRole.Permitted); set => SetIfChecked(value, PlayerRole.Permitted); }
    public bool DefaultIsBanned { get => Is(PlayerRole.Banned); set => SetIfChecked(value, PlayerRole.Banned); }
    public bool DefaultIsNone { get => SelectedAccount is not null && SelectedAccount.DisplayRole is null; set => SetIfChecked(value, PlayerRole.None); }

    private bool Is(PlayerRole role) => SelectedAccount?.DisplayRole == role;

    private void SetIfChecked(bool value, PlayerRole role)
    {
        if (value && SelectedAccount is { } row) _owner.SetDefaultRole(row.Key, role);
    }

    partial void OnSelectedAccountChanged(PlayerRowViewModel? value) => _owner.OnSelectionChanged(this);

    /// <summary>Re-raises the role-derived menu state after the selected row's role changed in place.</summary>
    internal void RefreshRoleFlags()
    {
        OnPropertyChanged(nameof(DefaultIsAdmin));
        OnPropertyChanged(nameof(DefaultIsPermitted));
        OnPropertyChanged(nameof(DefaultIsBanned));
        OnPropertyChanged(nameof(DefaultIsNone));
        RemoveCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private Task Add() => _owner.AddAsync(this);

    [RelayCommand(CanExecute = nameof(CanRemove))]
    private void Remove()
    {
        if (SelectedAccount is { } row) _owner.RemoveAccount(row.Key);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void ViewDetails()
    {
        if (SelectedAccount is { } row) _owner.RequestDetails(row.Key);
    }
}
