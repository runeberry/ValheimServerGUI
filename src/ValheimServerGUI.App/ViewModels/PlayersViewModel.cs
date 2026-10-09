using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ValheimServerGUI.App.Converters;
using ValheimServerGUI.App.ViewModels.Dialogs;
using ValheimServerGUI.Game;
using ValheimServerGUI.Localization;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Models;

namespace ValheimServerGUI.App.ViewModels;

/// <summary>
/// Players tab (§7.4): a live table fed by the shared player repository. Rows update from
/// <c>EntityUpdated</c>/<c>PlayerStatusChanged</c>; the relative "Since" column is recomputed every second
/// while the tab is visible. View-Details is enabled when a row is selected; Remove only when it is Offline.
///
/// Access management is a view/editor over the active profile's working state: each player's role on this
/// server is its <b>override</b> (stored on <see cref="ServerFormViewModel"/>) layered over its app-global
/// <b>default</b> (the Manage Players lists), resolved by <see cref="PlayerRoleResolver"/>. The tab shows the
/// effective role for the current mode (with <c>(*)</c> when an override replaces a default) and edits only
/// the override via the form — the "Set server role" submenu — (which trips the profile's dirty flag); the three list files are generated from
/// the resolved roles at server start, not edited here.
/// </summary>
public partial class PlayersViewModel : ViewModelBase
{
    private readonly IPlayerDataRepository _repo;
    private readonly ServerFormViewModel _form;
    private readonly IRuneberryApiClient _api;
    private readonly IUserPreferencesProvider _userPrefs;
    // Every known player's row (keyed + in arrival order); Players is the visible, same-ordered subset.
    private readonly Dictionary<string, PlayerRowViewModel> _rows = new();
    private readonly List<PlayerRowViewModel> _allRows = new();
    private readonly DispatcherTimer _sinceTimer;

    // The app-global player defaults, cached from user preferences and refreshed whenever they are saved.
    private IReadOnlyDictionary<string, PlayerDefaultEntry> _defaults;

    public PlayersViewModel(
        IPlayerDataRepository repo, ServerFormViewModel form, IRuneberryApiClient api, IUserPreferencesProvider userPrefs)
    {
        _repo = repo;
        _form = form;
        _api = api;
        _userPrefs = userPrefs;
        _defaults = CopyDefaults(userPrefs.LoadPreferences());

        _sinceTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _sinceTimer.Tick += (_, _) => RefreshSince();

        _repo.EntityUpdated += OnEntityUpdated;
        _repo.PlayerStatusChanged += OnEntityUpdated;
        _repo.EntityRemoved += OnEntityRemoved;
        _repo.DataUpdated += OnDataReloaded;
        _repo.DataReady += OnDataReloaded;

        // Roles live on the profile form: re-render when they change or the permitted-list mode flips.
        _form.RoleStateChanged += OnFormRolesChanged;
        _form.PropertyChanged += OnFormPropertyChanged;

        // Defaults change from the Manage Players dialog (any window): re-resolve every row.
        _userPrefs.PreferencesSaved += OnUserPreferencesSaved;

        Players.CollectionChanged += (_, _) => OnPropertyChanged(nameof(EmptyText));

        ReloadAll();
    }

    /// <summary>The rows shown: every known player, minus those banned on this server unless
    /// <see cref="ShowBannedPlayers"/> is on.</summary>
    public ObservableCollection<PlayerRowViewModel> Players { get; } = new();

    /// <summary>
    /// View filter only (off by default, not persisted): players whose effective role on this server is Banned
    /// are hidden unless this is on. The underlying data still covers every known player.
    /// </summary>
    [ObservableProperty] private bool _showBannedPlayers;

    partial void OnShowBannedPlayersChanged(bool value) => SyncVisibleRows();

    /// <summary>The table's empty-state hint, or null when it has rows.</summary>
    public string? EmptyText => Players.Count == 0 ? Strings.Players_EmptyText : null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanViewDetails), nameof(CanRemove))]
    [NotifyCanExecuteChangedFor(nameof(ViewDetailsCommand), nameof(RemoveCommand))]
    private PlayerRowViewModel? _selectedPlayer;

    partial void OnSelectedPlayerChanged(PlayerRowViewModel? value) => RaiseServerRoleMenu();

    public bool CanViewDetails => SelectedPlayer is not null;

    /// <summary>Remove is only allowed for an Offline player (§7.4).</summary>
    public bool CanRemove => SelectedPlayer is { IsOffline: true };

    // ----- "Set server role" submenu -----
    // Radio items for the selected player's override on this server: Admin / Permitted / Banned, or no override
    // (the fourth item, which falls back to the player's default role). Each reads the stored override and, when
    // checked, writes it through the form; a radio group's uncheck-the-others `false` writes are ignored. A server
    // never stores a "no role" override — the fourth item just removes the override.
    public bool ServerRoleIsAdmin { get => HasOverride(PlayerRole.Admin); set => SetOverrideIfChecked(value, PlayerRole.Admin); }
    public bool ServerRoleIsPermitted { get => HasOverride(PlayerRole.Permitted); set => SetOverrideIfChecked(value, PlayerRole.Permitted); }
    public bool ServerRoleIsBanned { get => HasOverride(PlayerRole.Banned); set => SetOverrideIfChecked(value, PlayerRole.Banned); }
    public bool ServerRoleIsDefault
    {
        get => SelectedPlayer is { } row && _form.GetOverride(row.Key) is null;
        set => SetOverrideIfChecked(value, null);
    }

    /// <summary>The fourth item's label: "Default role (Admin)" when there is a default to fall back on, else "None".</summary>
    public string ServerRoleDefaultLabel
        => SelectedPlayer is { } row && _defaults.TryGetValue(row.Key, out var entry)
            ? string.Format(Strings.Players_Menu_DefaultRole, EnumDisplayConverter.ToText(entry.DefaultRole))
            : Strings.Role_None;

    private bool HasOverride(PlayerRole role) => SelectedPlayer is { } row && _form.GetOverride(row.Key) == role;

    private void SetOverrideIfChecked(bool value, PlayerRole? role)
    {
        // Row re-render + menu refresh happen on the form's RoleStateChanged callback.
        if (value && SelectedPlayer is { } row) _form.SetRole(row.Player, role);
    }

    private void RaiseServerRoleMenu()
    {
        OnPropertyChanged(nameof(ServerRoleIsAdmin));
        OnPropertyChanged(nameof(ServerRoleIsPermitted));
        OnPropertyChanged(nameof(ServerRoleIsBanned));
        OnPropertyChanged(nameof(ServerRoleIsDefault));
        OnPropertyChanged(nameof(ServerRoleDefaultLabel));
    }

    /// <summary>Raised for View Player Details (opens Manage Players focused on the player).</summary>
    public event Action<PlayerInfo>? ViewDetailsRequested;

    /// <summary>Raised for Manage Players (the app-global player lists).</summary>
    public event Action? ManagePlayersRequested;

    /// <summary>Shows the Add Player dialog with the given role options; returns null on cancel. Wired by the window.</summary>
    public Func<AddPlayerOptions, Task<AddPlayerResult?>>? AddPlayerPrompt { get; set; }

    public void SetActive(bool active)
    {
        if (active)
        {
            RefreshSince();
            _sinceTimer.Start();
        }
        else
        {
            _sinceTimer.Stop();
        }
    }

    [RelayCommand(CanExecute = nameof(CanViewDetails))]
    private void ViewDetails()
    {
        if (SelectedPlayer is not null) ViewDetailsRequested?.Invoke(SelectedPlayer.Player);
    }

    [RelayCommand]
    private void ManagePlayers() => ManagePlayersRequested?.Invoke();

    [RelayCommand(CanExecute = nameof(CanRemove))]
    private void Remove()
    {
        if (SelectedPlayer is { IsOffline: true } row)
            _repo.Remove(row.Key);
    }

    [RelayCommand]
    private async Task AddPlayer()
    {
        if (AddPlayerPrompt is null) return;

        var result = await AddPlayerPrompt(AddPlayerOptions.ForServer(_form.UsePermittedList));
        if (result is null) return;

        // The role lands on this server's override (via the form, tripping the profile's dirty flag) or, as a
        // default role, in user preferences — saved immediately, which re-renders the tab and live-applies.
        var prefs = _userPrefs.LoadPreferences();
        var outcome = AddPlayerFlow.Apply(result, new RepoPlayerRecordStore(_repo), prefs.PlayerDefaults, _form.SetRole);
        if (outcome is null) return;
        if (result.AsDefault) _userPrefs.SavePreferences(prefs);

        // A brand-new record with no name yet: look it up the same way the join path does (fire-and-forget).
        if (outcome.IsNewRecord && string.IsNullOrWhiteSpace(outcome.Player.PlayerName))
            _ = _api.RequestPlayerInfoAsync(outcome.Player.Platform ?? string.Empty, outcome.Player.PlayerId ?? string.Empty);
    }

    private ResolvedRole Resolve(string key) => PlayerRoleResolver.Resolve(key, _form.PlayerRoles, _defaults);

    private static IReadOnlyDictionary<string, PlayerDefaultEntry> CopyDefaults(UserPreferences prefs)
        => new Dictionary<string, PlayerDefaultEntry>(prefs.PlayerDefaults);

    private void RefreshSince()
    {
        var now = DateTimeOffset.Now;
        foreach (var row in Players) row.RefreshSince(now);
    }

    // The effective role shown under the current mode (null = blank cell). Admin shows in both modes;
    // permitted only in permitted-list mode; banned only when the ban list is in effect. Mirrors
    // PlayerAccessListRules so the table reads the same rules the files are generated from.
    private PlayerRole? ModeFiltered(PlayerRole role) => role switch
    {
        PlayerRole.Admin => PlayerRole.Admin,
        PlayerRole.Permitted => _form.UsePermittedList ? PlayerRole.Permitted : null,
        PlayerRole.Banned => _form.UsePermittedList ? null : PlayerRole.Banned,
        _ => null,
    };

    // A role that has no effect in this mode renders blank — including its "(*)" marker, since there is no role
    // shown for the marker to qualify.
    private void ApplyRole(PlayerRowViewModel row)
    {
        var resolved = Resolve(row.Key);
        var shown = ModeFiltered(resolved.Effective);
        row.DisplayRole = shown;
        row.ShowsOverrideMarker = shown is not null && resolved.ShowsOverrideMarker;
    }

    private void OnFormRolesChanged(object? sender, EventArgs e) => RunOnUi(RerenderAccess);

    private void OnUserPreferencesSaved(object? sender, UserPreferences prefs) => RunOnUi(() =>
    {
        _defaults = CopyDefaults(prefs);
        RerenderAccess();
    });

    private void OnFormPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ServerFormViewModel.UsePermittedList)) RunOnUi(RerenderAccess);
    }

    // Re-derive every row's displayed role and visibility, and the selected row's role menu.
    private void RerenderAccess()
    {
        foreach (var row in _allRows) ApplyRole(row);
        SyncVisibleRows(); // a role change can ban or unban a player
        RaiseServerRoleMenu();
    }

    private void OnEntityUpdated(object? sender, PlayerInfo player) => RunOnUi(() => Upsert(player));

    private void OnEntityRemoved(object? sender, PlayerInfo player) => RunOnUi(() =>
    {
        if (_rows.Remove(player.Key, out var row))
        {
            _allRows.Remove(row);
            Players.Remove(row);
            if (ReferenceEquals(SelectedPlayer, row)) SelectedPlayer = null;
        }
    });

    private void OnDataReloaded(object? sender, EventArgs e) => RunOnUi(ReloadAll);

    private void Upsert(PlayerInfo player)
    {
        if (_rows.TryGetValue(player.Key, out var row))
        {
            row.Update(player);
            ApplyRole(row);
            if (ReferenceEquals(SelectedPlayer, row))
                OnPropertyChanged(nameof(CanRemove)); // offline-ness may have changed
        }
        else
        {
            var newRow = new PlayerRowViewModel(player);
            ApplyRole(newRow);
            _rows[player.Key] = newRow;
            _allRows.Add(newRow);
        }
        SyncVisibleRows();
    }

    private void ReloadAll()
    {
        _rows.Clear();
        _allRows.Clear();
        Players.Clear();
        foreach (var player in _repo.Data)
        {
            var row = new PlayerRowViewModel(player);
            ApplyRole(row);
            _rows[player.Key] = row;
            _allRows.Add(row);
        }
        SyncVisibleRows();
    }

    private bool IsVisible(PlayerRowViewModel row)
        => ShowBannedPlayers || Resolve(row.Key).Effective != PlayerRole.Banned;

    // Brings Players in line with the filter by walking the master list: Players is always an ordered subsequence
    // of _allRows, so each row is inserted/removed in place (no reset), keeping selection and sort intact.
    private void SyncVisibleRows()
    {
        var i = 0;
        foreach (var row in _allRows)
        {
            var present = i < Players.Count && ReferenceEquals(Players[i], row);
            if (IsVisible(row))
            {
                if (!present) Players.Insert(i, row);
                i++;
            }
            else if (present)
            {
                Players.RemoveAt(i);
                if (ReferenceEquals(SelectedPlayer, row)) SelectedPlayer = null;
            }
        }
    }

    protected override void DisposeCore()
    {
        _sinceTimer.Stop();
        _repo.EntityUpdated -= OnEntityUpdated;
        _repo.PlayerStatusChanged -= OnEntityUpdated;
        _repo.EntityRemoved -= OnEntityRemoved;
        _repo.DataUpdated -= OnDataReloaded;
        _repo.DataReady -= OnDataReloaded;
        _form.RoleStateChanged -= OnFormRolesChanged;
        _form.PropertyChanged -= OnFormPropertyChanged;
        _userPrefs.PreferencesSaved -= OnUserPreferencesSaved;
    }
}
