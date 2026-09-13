using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ValheimServerGUI.Game;

namespace ValheimServerGUI.App.ViewModels.Dialogs;

/// <summary>
/// Player Details dialog (§7.4): read-only identity + an editable display-name override (0–64) and
/// known-characters list (added names are flagged <c>matchConfident=true</c>). Pulls fresh data from the
/// repo by key; unsaved edits are guarded on close.
/// </summary>
public partial class PlayerDetailsViewModel : ModalEditViewModel
{
    private readonly IPlayerDataRepository _repo;
    private readonly string _key;
    private readonly Dictionary<string, bool> _originalConfidence = new();

    public PlayerDetailsViewModel(IPlayerDataRepository repo, string playerKey)
    {
        _repo = repo;
        _key = playerKey;
        Load();
    }

    // Read-only identity.
    [ObservableProperty] private string _platform = string.Empty;
    [ObservableProperty] private string _playerId = string.Empty;
    [ObservableProperty] private string _zdoId = string.Empty;
    [ObservableProperty] private string _latestCharacter = string.Empty;
    [ObservableProperty] private string _status = string.Empty;
    [ObservableProperty] private string _statusChanged = string.Empty;

    // Editable.
    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private string? _selectedCharacter;

    public ObservableCollection<string> Characters { get; } = new();

    private void Load() => LoadClean(() =>
    {
        var player = _repo.FindById(_key);
        if (player is null) return;

        LoadIdentity(player);
        DisplayName = player.PlayerName ?? string.Empty;

        Characters.Clear();
        _originalConfidence.Clear();
        foreach (var c in player.Characters ?? new List<PlayerInfo.CharacterInfo>())
        {
            if (string.IsNullOrWhiteSpace(c.CharacterName)) continue;
            Characters.Add(c.CharacterName);
            _originalConfidence[c.CharacterName] = c.MatchConfident;
        }
    });

    // The read-only identity/status fields (everything except the editable display name + character list).
    private void LoadIdentity(PlayerInfo player)
    {
        Platform = player.Platform ?? string.Empty;
        PlayerId = player.PlayerId ?? string.Empty;
        ZdoId = player.ZdoId ?? string.Empty;
        LatestCharacter = player.LastStatusCharacter ?? string.Empty;
        Status = player.PlayerStatus.ToString();
        StatusChanged = player.LastStatusChange == default ? string.Empty : player.LastStatusChange.ToString("G");
    }

    /// <summary>Re-reads the current identity/status from the repo (status changes while the dialog is open).
    /// Unlike the WinForms Refresh button, this leaves the editable display name + characters untouched, so it
    /// can't discard unsaved edits.</summary>
    [RelayCommand]
    private void Refresh()
    {
        if (_repo.FindById(_key) is { } player) LoadIdentity(player);
    }

    public override void ApplyDefaults() { /* Player Details has no defaults to restore. */ }

    public void AddCharacter(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || Characters.Contains(name)) return;
        Characters.Add(name);
        IsDirty = true;
    }

    public void RenameCharacter(string oldName, string newName)
    {
        var index = Characters.IndexOf(oldName);
        if (index < 0 || string.IsNullOrWhiteSpace(newName)) return;
        Characters[index] = newName;
        IsDirty = true;
    }

    [RelayCommand]
    private void RemoveCharacter()
    {
        if (SelectedCharacter is not null && Characters.Remove(SelectedCharacter))
            IsDirty = true;
    }

    public void Save()
    {
        var player = _repo.FindById(_key);
        if (player is null) return;

        player.PlayerName = string.IsNullOrWhiteSpace(DisplayName) ? null : DisplayName;
        player.Characters = Characters
            .Select(name => new PlayerInfo.CharacterInfo
            {
                CharacterName = name,
                // Added names are confident; loaded ones keep their original flag.
                MatchConfident = _originalConfidence.TryGetValue(name, out var confident) ? confident : true,
            })
            .ToList();

        _repo.Upsert(player);
    }
}
