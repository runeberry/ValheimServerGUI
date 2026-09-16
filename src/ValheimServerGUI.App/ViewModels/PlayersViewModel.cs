using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools.Models;

namespace ValheimServerGUI.App.ViewModels;

/// <summary>The identity + list choices returned by the "Add by ID" dialog.</summary>
public record AddByIdResult(string Platform, string PlayerId, bool Admin, bool Banned, bool Permitted);

/// <summary>
/// Players tab (§7.4): a live table fed by the shared player repository. Rows update from
/// <c>EntityUpdated</c>/<c>PlayerStatusChanged</c>; the relative "Since" column is recomputed every second
/// while the tab is visible. View-Details is enabled when a row is selected; Remove only when it is Offline.
///
/// Access-list management (admin/ban/permit) is <b>profile-scoped</b>: <see cref="SetSaveDataFolder"/> points
/// the tab at the active profile's savedir (pushed by the window on retarget), and the three list files there
/// drive each row's membership glyphs and the toggle commands.
/// </summary>
public partial class PlayersViewModel : ViewModelBase
{
    private readonly IPlayerDataRepository _repo;
    private readonly IPlayerAccessListService _accessLists;
    private readonly Dictionary<string, PlayerRowViewModel> _rows = new();
    private readonly DispatcherTimer _sinceTimer;

    private string? _saveDataFolder;
    private IReadOnlyList<string> _adminEntries = Array.Empty<string>();
    private IReadOnlyList<string> _bannedEntries = Array.Empty<string>();
    private IReadOnlyList<string> _permittedEntries = Array.Empty<string>();

    public PlayersViewModel(IPlayerDataRepository repo, IPlayerAccessListService accessLists)
    {
        _repo = repo;
        _accessLists = accessLists;

        _sinceTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _sinceTimer.Tick += (_, _) => RefreshSince();

        _repo.EntityUpdated += OnEntityUpdated;
        _repo.PlayerStatusChanged += OnEntityUpdated;
        _repo.EntityRemoved += OnEntityRemoved;
        _repo.DataUpdated += OnDataReloaded;
        _repo.DataReady += OnDataReloaded;

        ReloadAll();
    }

    public ObservableCollection<PlayerRowViewModel> Players { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanViewDetails), nameof(CanRemove), nameof(CanManageAccess),
        nameof(AdminToggleLabel), nameof(BanToggleLabel), nameof(PermitToggleLabel))]
    [NotifyCanExecuteChangedFor(nameof(ViewDetailsCommand), nameof(RemoveCommand),
        nameof(ToggleAdminCommand), nameof(ToggleBanCommand), nameof(TogglePermitCommand))]
    private PlayerRowViewModel? _selectedPlayer;

    // Context-menu labels reflect the selected row's current membership (derived, never mirrored).
    public string AdminToggleLabel => SelectedPlayer?.IsAdmin == true ? "Remove admin" : "Make admin";
    public string BanToggleLabel => SelectedPlayer?.IsBanned == true ? "Unban player" : "Ban player";
    public string PermitToggleLabel => SelectedPlayer?.IsPermitted == true ? "Remove from permitted" : "Add to permitted";

    public bool CanViewDetails => SelectedPlayer is not null;

    /// <summary>Remove is only allowed for an Offline player (§7.4).</summary>
    public bool CanRemove => SelectedPlayer is { IsOffline: true };

    /// <summary>Access-list toggles need a selected player and a configured (profile) savedir.</summary>
    public bool CanManageAccess => SelectedPlayer is not null && !string.IsNullOrWhiteSpace(_saveDataFolder);

    /// <summary>True once a profile savedir is set, so "Add by ID…" can be offered.</summary>
    public bool CanAddById => !string.IsNullOrWhiteSpace(_saveDataFolder);

    /// <summary>Raised for View Player Details.</summary>
    public event Action<PlayerInfo>? ViewDetailsRequested;

    /// <summary>Shows the "Add by ID" dialog; returns null on cancel. Wired by the window.</summary>
    public Func<Task<AddByIdResult?>>? AddByIdPrompt { get; set; }

    /// <summary>Non-blocking notice (e.g. "ban overrides the other lists"). Wired by the window.</summary>
    public Action<string>? NoticeReported { get; set; }

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

    /// <summary>
    /// Points the tab at a profile's save-data folder (null when unconfigured). Re-reads the three list files
    /// and refreshes every row's membership. Called by the window on profile retarget.
    /// </summary>
    public void SetSaveDataFolder(string? saveDataFolder)
    {
        _saveDataFolder = string.IsNullOrWhiteSpace(saveDataFolder) ? null : saveDataFolder;
        ReloadListCache();
        foreach (var row in Players) ApplyMembership(row);
        OnPropertyChanged(nameof(CanManageAccess));
        OnPropertyChanged(nameof(CanAddById));
        AddByIdCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanViewDetails))]
    private void ViewDetails()
    {
        if (SelectedPlayer is not null) ViewDetailsRequested?.Invoke(SelectedPlayer.Player);
    }

    [RelayCommand(CanExecute = nameof(CanRemove))]
    private void Remove()
    {
        if (SelectedPlayer is { IsOffline: true } row)
            _repo.Remove(row.Key);
    }

    [RelayCommand(CanExecute = nameof(CanManageAccess))]
    private void ToggleAdmin() => Toggle(PlayerAccessList.Admin);

    [RelayCommand(CanExecute = nameof(CanManageAccess))]
    private void ToggleBan() => Toggle(PlayerAccessList.Banned);

    [RelayCommand(CanExecute = nameof(CanManageAccess))]
    private void TogglePermit() => Toggle(PlayerAccessList.Permitted);

    [RelayCommand(CanExecute = nameof(CanAddById))]
    private async Task AddById()
    {
        if (AddByIdPrompt is null || _saveDataFolder is null) return;

        var result = await AddByIdPrompt();
        if (result is null) return;
        if (!PlayerPlatforms.TryGetValidPlatform(result.Platform, out var platform) || platform is null) return;
        if (string.IsNullOrWhiteSpace(result.PlayerId)) return;

        var playerId = result.PlayerId.Trim();

        // Create/annotate a repo record so the person appears in the table. For a manually-entered ID the
        // normalized platform name IS the canonical write token, so PlatformRaw = the platform name.
        var key = $"{platform}:{playerId}";
        var player = _repo.FindById(key) ?? new PlayerInfo
        {
            Platform = platform,
            PlatformRaw = platform,
            PlayerId = playerId,
            PlayerStatus = PlayerStatus.Offline,
            LastStatusChange = DateTimeOffset.UtcNow,
        };
        if (string.IsNullOrWhiteSpace(player.PlatformRaw)) player.PlatformRaw = platform;

        if (result.Admin) _accessLists.Add(_saveDataFolder, PlayerAccessList.Admin, player);
        if (result.Banned) _accessLists.Add(_saveDataFolder, PlayerAccessList.Banned, player);
        if (result.Permitted) _accessLists.Add(_saveDataFolder, PlayerAccessList.Permitted, player);

        WarnIfBanOverrides(result.Banned, result.Admin, result.Permitted);

        ReloadListCache();
        _repo.Upsert(player); // OnEntityUpdated adds/updates the row and applies membership from the cache.
    }

    // Toggles the selected player's membership in one list, then refreshes the cache + that row.
    private void Toggle(PlayerAccessList list)
    {
        if (SelectedPlayer is not { } row || _saveDataFolder is null) return;

        var isMember = list switch
        {
            PlayerAccessList.Admin => row.IsAdmin,
            PlayerAccessList.Banned => row.IsBanned,
            _ => row.IsPermitted,
        };

        if (isMember)
        {
            _accessLists.Remove(_saveDataFolder, list, row.Player);
        }
        else
        {
            _accessLists.Add(_saveDataFolder, list, row.Player);
            if (list == PlayerAccessList.Banned)
                WarnIfBanOverrides(true, row.IsAdmin, row.IsPermitted);
        }

        ReloadListCache();
        ApplyMembership(row);

        // The selected row's membership just changed, so refresh the context-menu verb labels.
        OnPropertyChanged(nameof(AdminToggleLabel));
        OnPropertyChanged(nameof(BanToggleLabel));
        OnPropertyChanged(nameof(PermitToggleLabel));
    }

    private void WarnIfBanOverrides(bool banning, bool isAdmin, bool isPermitted)
    {
        if (banning && (isAdmin || isPermitted))
        {
            NoticeReported?.Invoke(
                "This player is banned. A ban overrides the admin and permitted lists — the player will be " +
                "kicked and kept out regardless of those.");
        }
    }

    private void RefreshSince()
    {
        foreach (var row in Players) row.RefreshSince();
    }

    private void ReloadListCache()
    {
        if (_saveDataFolder is null)
        {
            _adminEntries = _bannedEntries = _permittedEntries = Array.Empty<string>();
            return;
        }

        _adminEntries = _accessLists.ReadEntries(_saveDataFolder, PlayerAccessList.Admin);
        _bannedEntries = _accessLists.ReadEntries(_saveDataFolder, PlayerAccessList.Banned);
        _permittedEntries = _accessLists.ReadEntries(_saveDataFolder, PlayerAccessList.Permitted);
    }

    private void ApplyMembership(PlayerRowViewModel row) => row.SetMembership(
        _accessLists.Contains(_adminEntries, row.Player),
        _accessLists.Contains(_bannedEntries, row.Player),
        _accessLists.Contains(_permittedEntries, row.Player));

    private void OnEntityUpdated(object? sender, PlayerInfo player) => RunOnUi(() => Upsert(player));

    private void OnEntityRemoved(object? sender, PlayerInfo player) => RunOnUi(() =>
    {
        if (_rows.Remove(player.Key, out var row))
        {
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
            ApplyMembership(row);
            if (ReferenceEquals(SelectedPlayer, row))
                OnPropertyChanged(nameof(CanRemove)); // offline-ness may have changed
        }
        else
        {
            var newRow = new PlayerRowViewModel(player);
            ApplyMembership(newRow);
            _rows[player.Key] = newRow;
            Players.Add(newRow);
        }
    }

    private void ReloadAll()
    {
        _rows.Clear();
        Players.Clear();
        foreach (var player in _repo.Data)
        {
            var row = new PlayerRowViewModel(player);
            ApplyMembership(row);
            _rows[player.Key] = row;
            Players.Add(row);
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
    }
}
