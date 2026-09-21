using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.App.ViewModels.Dialogs;

/// <summary>
/// Player Details dialog (§7.4): read-only identity + an editable display-name override (0–64) and
/// known-characters table (name + derived Status/Since; added names are flagged <c>matchConfident=true</c>).
/// Pulls fresh data from the repo by key; unsaved edits are guarded on close.
/// </summary>
public partial class PlayerDetailsViewModel : ModalEditViewModel
{
    private readonly IPlayerDataRepository _repo;
    private readonly IRuneberryApiClient? _api;
    private readonly string _key;

    public PlayerDetailsViewModel(IPlayerDataRepository repo, string playerKey, IRuneberryApiClient? api = null)
    {
        _repo = repo;
        _api = api;
        _key = playerKey;
        // Selecting a character in the table is view state, not an edit — it must not trip the unsaved-changes
        // guard (the actual edits are AddCharacter/RenameCharacter/RemoveCharacter and the display-name field).
        IgnoreForDirty(nameof(SelectedCharacter));
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
    [ObservableProperty] private CharacterRowViewModel? _selectedCharacter;

    /// <summary>The display name for the read-only Player Name row; "(unknown)" when none is set.</summary>
    public string DisplayNameOrUnknown =>
        string.IsNullOrWhiteSpace(DisplayName) ? "(unknown)" : DisplayName;

    /// <summary>Caption for the platform-id row — "Steam ID" or "Xbox ID" per the player's platform.</summary>
    public string PlatformIdLabel =>
        string.Equals(Platform, "Xbox", StringComparison.OrdinalIgnoreCase) ? "Xbox ID:" : "Steam ID:";

    public ObservableCollection<CharacterRowViewModel> Characters { get; } = new();

    private void Load() => LoadClean(() =>
    {
        var player = _repo.FindById(_key);
        if (player is null) return;

        LoadIdentity(player);
        DisplayName = player.PlayerName ?? string.Empty;

        Characters.Clear();
        var now = DateTimeOffset.Now;
        foreach (var c in player.Characters ?? new List<PlayerInfo.CharacterInfo>())
        {
            if (string.IsNullOrWhiteSpace(c.CharacterName)) continue;
            var row = new CharacterRowViewModel(c.CharacterName!, c.MatchConfident, c.LastSeen);
            row.Refresh(player, now);
            Characters.Add(row);
        }
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
        var now = DateTimeOffset.Now;
        foreach (var row in Characters) row.Refresh(player, now);

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

    public void AddCharacter(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || Characters.Any(c => c.CharacterName == name)) return;
        Characters.Add(new CharacterRowViewModel(name));
        IsDirty = true;
    }

    public void RenameCharacter(string oldName, string newName)
    {
        var row = Characters.FirstOrDefault(c => c.CharacterName == oldName);
        if (row is null || string.IsNullOrWhiteSpace(newName)) return;
        row.CharacterName = newName;
        IsDirty = true;
    }

    [RelayCommand]
    private void RemoveCharacter()
    {
        if (SelectedCharacter is { } row && Characters.Remove(row))
            IsDirty = true;
    }

    public void Save()
    {
        var player = _repo.FindById(_key);
        if (player is null) return;

        player.PlayerName = string.IsNullOrWhiteSpace(DisplayName) ? null : DisplayName;
        player.Characters = Characters
            .Select(row => new PlayerInfo.CharacterInfo
            {
                CharacterName = row.CharacterName,
                // Added names are confident; loaded ones keep their original flag and last-seen time.
                MatchConfident = row.MatchConfident,
                LastSeen = row.LastSeen,
            })
            .ToList();

        _repo.Upsert(player);
    }
}
