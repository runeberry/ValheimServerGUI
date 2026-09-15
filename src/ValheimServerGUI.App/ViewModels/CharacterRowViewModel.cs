using System;
using CommunityToolkit.Mvvm.ComponentModel;
using ValheimServerGUI.App.Converters;
using ValheimServerGUI.Game;

namespace ValheimServerGUI.App.ViewModels;

/// <summary>
/// One row in the Player Details "Known Characters" table (§7.4): the character's name plus a derived
/// Status and "Since". Only the player's currently-active character (<see cref="PlayerInfo.LastStatusCharacter"/>)
/// carries the live status; every other character is Offline. "Since" comes from the optional per-character
/// <see cref="PlayerInfo.CharacterInfo.LastSeen"/> and is blank for characters recorded before that field
/// existed. <see cref="MatchConfident"/>/<see cref="LastSeen"/> are carried so a save round-trips them.
/// </summary>
public partial class CharacterRowViewModel : ObservableObject
{
    public CharacterRowViewModel(string characterName, bool matchConfident = true, DateTimeOffset? lastSeen = null)
    {
        CharacterName = characterName;
        MatchConfident = matchConfident;
        LastSeen = lastSeen;
        StatusText = Status.ToString();
    }

    [ObservableProperty] private string _characterName;
    [ObservableProperty] private PlayerStatus _status;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private string _sinceText = string.Empty;

    /// <summary>Preserved from the cache so a save doesn't drop the low-confidence flag.</summary>
    public bool MatchConfident { get; set; }

    /// <summary>Preserved from the cache so a save doesn't drop the last-seen timestamp.</summary>
    public DateTimeOffset? LastSeen { get; set; }

    /// <summary>Re-derives the Status/Since columns from the owning player's current state.</summary>
    public void Refresh(PlayerInfo player, DateTimeOffset now)
    {
        var isActive = CharacterName == player.LastStatusCharacter && player.PlayerStatus != PlayerStatus.Offline;
        Status = isActive ? player.PlayerStatus : PlayerStatus.Offline;
        StatusText = Status.ToString();
        SinceText = LastSeen is { } seen ? RelativeTimeConverter.Format(seen, now) : string.Empty;
    }
}
