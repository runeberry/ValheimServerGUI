using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ValheimServerGUI.Game;
using ValheimServerGUI.Localization;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Models;

namespace ValheimServerGUI.App.ViewModels.Dialogs;

/// <summary>
/// Player Details dialog (§7.4): read-only identity + an editable display-name override (0–64) and
/// known-characters table (<see cref="KnownCharactersViewModel"/>). Edits are read from and written to an
/// <see cref="IPlayerRecordStore"/> (the live repo by default; Manage Players passes its staged copy); live
/// status and name lookups always come from the repo. Unsaved edits are guarded on close.
/// </summary>
public partial class PlayerDetailsViewModel : ModalEditViewModel
{
    private readonly IPlayerDataRepository _repo;
    private readonly IPlayerRecordStore _store;
    private readonly IRuneberryApiClient? _api;
    private readonly string _key;

    public PlayerDetailsViewModel(
        IPlayerDataRepository repo, string playerKey, IRuneberryApiClient? api = null, IPlayerRecordStore? store = null)
    {
        _repo = repo;
        _store = store ?? new RepoPlayerRecordStore(repo);
        _api = api;
        _key = playerKey;
        KnownCharacters.Edited += (_, _) => IsDirty = true;
        Load();
    }

    // Read-only identity.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlatformIdLabel))]
    private string _platform = string.Empty;
    [ObservableProperty] private string _playerId = string.Empty;
    [ObservableProperty] private string _zdoId = string.Empty;

    // Editable.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayNameOrUnknown))]
    private string _displayName = string.Empty;

    /// <summary>The editable known-characters table.</summary>
    public KnownCharactersViewModel KnownCharacters { get; } = new();

    /// <summary>The display name for the read-only Player Name row; Strings.PlayerDetails_UnknownName when none is set.</summary>
    public string DisplayNameOrUnknown =>
        string.IsNullOrWhiteSpace(DisplayName) ? Strings.PlayerDetails_UnknownName : DisplayName;

    /// <summary>Caption for the platform-id row — "Steam ID" or "Xbox ID" per the player's platform.</summary>
    public string PlatformIdLabel =>
        string.Equals(Platform, PlayerPlatforms.Xbox, StringComparison.OrdinalIgnoreCase) ? Strings.PlayerDetails_XboxId_Label : Strings.PlayerDetails_SteamId_Label;

    private void Load() => LoadClean(() =>
    {
        var player = _store.FindById(_key);
        if (player is null) return;

        LoadIdentity(player);
        DisplayName = player.PlayerName ?? string.Empty;
        KnownCharacters.Load(player);
    });

    // The read-only identity fields (everything except the editable display name + character table).
    private void LoadIdentity(PlayerInfo player)
    {
        Platform = player.Platform ?? string.Empty;
        PlayerId = player.PlayerId ?? string.Empty;
        ZdoId = player.ZdoId ?? string.Empty;
    }

    /// <summary>Re-reads the current identity from the repo and re-derives each character's Status/Since
    /// (status changes while the dialog is open). When the player name is still unknown, it also fires a
    /// fresh platform name lookup and fills the name in once it resolves — but only while the field is empty,
    /// so it never overwrites a name the user typed. Leaves the editable display name (when set) + character
    /// edits untouched, so it can't discard unsaved changes.</summary>
    [RelayCommand]
    private async Task Refresh()
    {
        if (_repo.FindById(_key) is not { } player) return;
        LoadIdentity(player);
        KnownCharacters.Refresh(player);

        // Only look the name up when it's currently unknown; a lookup writes to the same PlayerName field the
        // user can override, so firing it unconditionally could clobber a custom name.
        if (_api is null || !string.IsNullOrWhiteSpace(DisplayName)) return;

        // The await resumes on the UI thread; the repo updates PlayerName synchronously inside the request, so
        // it's populated by the time we return. Re-check the field is still empty in case the user typed while
        // the lookup was in flight.
        await _api.RequestPlayerInfoAsync(player.Platform ?? string.Empty, player.PlayerId ?? string.Empty);

        if (string.IsNullOrWhiteSpace(DisplayName)
            && _repo.FindById(_key)?.PlayerName is { } resolved
            && !string.IsNullOrWhiteSpace(resolved))
        {
            ApplyWithoutDirtying(() => DisplayName = resolved);
        }
    }

    public override void ApplyDefaults() { /* Player Details has no defaults to restore. */ }

    public void Save()
    {
        var player = _store.FindById(_key);
        if (player is null) return;

        player.PlayerName = string.IsNullOrWhiteSpace(DisplayName) ? null : DisplayName;
        KnownCharacters.WriteTo(player);

        _store.Upsert(player);
    }
}
