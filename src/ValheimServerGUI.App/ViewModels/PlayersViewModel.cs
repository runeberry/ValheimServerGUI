using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Models;

namespace ValheimServerGUI.App.ViewModels;

/// <summary>The identity + single role returned by the "Add by ID" dialog.</summary>
public record AddByIdResult(string Platform, string PlayerId, PlayerRole Role);

/// <summary>
/// Players tab (§7.4): a live table fed by the shared player repository. Rows update from
/// <c>EntityUpdated</c>/<c>PlayerStatusChanged</c>; the relative "Since" column is recomputed every second
/// while the tab is visible. View-Details is enabled when a row is selected; Remove only when it is Offline.
///
/// Access management is now a view/editor over the active profile's working state: each player has a single
/// <see cref="PlayerRole"/> stored on <see cref="ServerFormViewModel"/> plus a per-profile
/// <c>UsePermittedList</c> flag. The tab shows the <b>effective</b> role for the current mode and rewrites the
/// stored role via the form (which trips the profile's dirty flag); the three list files are generated from
/// those roles at server start, not edited here.
/// </summary>
public partial class PlayersViewModel : ViewModelBase
{
    private readonly IPlayerDataRepository _repo;
    private readonly ServerFormViewModel _form;
    private readonly IRuneberryApiClient _api;
    private readonly Dictionary<string, PlayerRowViewModel> _rows = new();
    private readonly DispatcherTimer _sinceTimer;

    public PlayersViewModel(IPlayerDataRepository repo, ServerFormViewModel form, IRuneberryApiClient api)
    {
        _repo = repo;
        _form = form;
        _api = api;

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

        ReloadAll();
    }

    public ObservableCollection<PlayerRowViewModel> Players { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanViewDetails), nameof(CanRemove), nameof(CanManageAccess),
        nameof(AdminToggleLabel), nameof(BanToggleLabel), nameof(PermitToggleLabel))]
    [NotifyCanExecuteChangedFor(nameof(ViewDetailsCommand), nameof(RemoveCommand),
        nameof(ToggleAdminCommand), nameof(ToggleBanCommand), nameof(TogglePermitCommand))]
    private PlayerRowViewModel? _selectedPlayer;

    // Context-menu labels reflect the selected row's STORED role (derived, never mirrored). Positive verbs set
    // the role; negative verbs clear it to none (single-role model).
    public string AdminToggleLabel => SelectedRole == PlayerRole.Admin ? "Remove admin" : "Make admin";
    public string BanToggleLabel => SelectedRole == PlayerRole.Banned ? "Unban player" : "Ban player";
    public string PermitToggleLabel => SelectedRole == PlayerRole.Permitted ? "Revoke join permission" : "Add to permitted";

    // The ban/permit verbs only apply to the mode that actually uses their list: ban when not using the
    // permitted list, permit when using it. The admin pair is relevant in both modes.
    public bool ShowBanToggle => !_form.UsePermittedList;
    public bool ShowPermitToggle => _form.UsePermittedList;

    public bool CanViewDetails => SelectedPlayer is not null;

    /// <summary>Remove is only allowed for an Offline player (§7.4).</summary>
    public bool CanRemove => SelectedPlayer is { IsOffline: true };

    /// <summary>Role toggles need a selected player (roles are profile working state, always available).</summary>
    public bool CanManageAccess => SelectedPlayer is not null;

    // The selected player's stored role for the active profile (null when it has none).
    private PlayerRole? SelectedRole => SelectedPlayer is { } row ? _form.GetRole(row.Key) : null;

    /// <summary>Raised for View Player Details.</summary>
    public event Action<PlayerInfo>? ViewDetailsRequested;

    /// <summary>
    /// Shows the "Add by ID" dialog for the given mode (<c>usePermittedList</c>); returns null on cancel. The
    /// mode drives which role options the dialog offers. Wired by the window.
    /// </summary>
    public Func<bool, Task<AddByIdResult?>>? AddByIdPrompt { get; set; }

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

    [RelayCommand(CanExecute = nameof(CanRemove))]
    private void Remove()
    {
        if (SelectedPlayer is { IsOffline: true } row)
            _repo.Remove(row.Key);
    }

    [RelayCommand(CanExecute = nameof(CanManageAccess))]
    private void ToggleAdmin() => Toggle(PlayerRole.Admin);

    [RelayCommand(CanExecute = nameof(CanManageAccess))]
    private void ToggleBan() => Toggle(PlayerRole.Banned);

    [RelayCommand(CanExecute = nameof(CanManageAccess))]
    private void TogglePermit() => Toggle(PlayerRole.Permitted);

    [RelayCommand]
    private async Task AddById()
    {
        if (AddByIdPrompt is null) return;

        var result = await AddByIdPrompt(_form.UsePermittedList);
        if (result is null) return;
        if (!PlayerPlatforms.TryGetValidPlatform(result.Platform, out var platform) || platform is null) return;
        if (string.IsNullOrWhiteSpace(result.PlayerId)) return;

        var playerId = result.PlayerId.Trim();

        // Create/annotate a repo record so the person appears in the table. For a manually-entered ID the
        // normalized platform name IS the canonical write token, so PlatformRaw = the platform name.
        var key = $"{platform}:{playerId}";
        var existing = _repo.FindById(key);
        var player = existing ?? new PlayerInfo
        {
            Platform = platform,
            PlatformRaw = platform,
            PlayerId = playerId,
            PlayerStatus = PlayerStatus.Offline,
            LastStatusChange = DateTimeOffset.UtcNow,
        };
        if (string.IsNullOrWhiteSpace(player.PlatformRaw)) player.PlatformRaw = platform;

        // The dialog picks exactly one role for the active mode; store it directly.
        _form.SetRole(player, result.Role);

        _repo.Upsert(player); // OnEntityUpdated adds/updates the row; ApplyRole reads the role back from the form.

        // A brand-new record has no name yet: look it up the same way the join path does (fire-and-forget).
        if (existing is null)
            _ = _api.RequestPlayerInfoAsync(platform, playerId);
    }

    // Flips the selected player's stored role for one verb: set it when absent, clear it when already set.
    private void Toggle(PlayerRole role)
    {
        if (SelectedPlayer is not { } row) return;

        var current = _form.GetRole(row.Key);
        // Positive verb sets the role (overwriting any prior one); the negative verb clears it to none.
        _form.SetRole(row.Player, current == role ? null : role);
        // Row re-render + label refresh happen on the form's RoleStateChanged callback.
    }

    private void RefreshSince()
    {
        foreach (var row in Players) row.RefreshSince();
    }

    // The role shown for a player under the current mode (null = blank cell). Admin shows in both modes;
    // permitted only in permitted-list mode; banned only when the ban list is in effect. Mirrors
    // PlayerAccessListRules so the table reads the same rules the files are generated from.
    private PlayerRole? DisplayRoleFor(string key) => _form.GetRole(key) switch
    {
        PlayerRole.Admin => PlayerRole.Admin,
        PlayerRole.Permitted => _form.UsePermittedList ? PlayerRole.Permitted : null,
        PlayerRole.Banned => _form.UsePermittedList ? null : PlayerRole.Banned,
        _ => null,
    };

    private void ApplyRole(PlayerRowViewModel row) => row.DisplayRole = DisplayRoleFor(row.Key);

    private void OnFormRolesChanged(object? sender, EventArgs e) => RunOnUi(RerenderAccess);

    private void OnFormPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ServerFormViewModel.UsePermittedList)) RunOnUi(RerenderAccess);
    }

    // Re-derive every row's displayed role and refresh the mode-dependent menu labels/visibility.
    private void RerenderAccess()
    {
        foreach (var row in Players) ApplyRole(row);
        OnPropertyChanged(nameof(AdminToggleLabel));
        OnPropertyChanged(nameof(BanToggleLabel));
        OnPropertyChanged(nameof(PermitToggleLabel));
        OnPropertyChanged(nameof(ShowBanToggle));
        OnPropertyChanged(nameof(ShowPermitToggle));
    }

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
            ApplyRole(row);
            if (ReferenceEquals(SelectedPlayer, row))
                OnPropertyChanged(nameof(CanRemove)); // offline-ness may have changed
        }
        else
        {
            var newRow = new PlayerRowViewModel(player);
            ApplyRole(newRow);
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
            ApplyRole(row);
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
        _form.RoleStateChanged -= OnFormRolesChanged;
        _form.PropertyChanged -= OnFormPropertyChanged;
    }
}
