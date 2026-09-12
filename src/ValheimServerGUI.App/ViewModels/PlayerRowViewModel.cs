using System;
using CommunityToolkit.Mvvm.ComponentModel;
using ValheimServerGUI.App.Converters;
using ValheimServerGUI.Game;

namespace ValheimServerGUI.App.ViewModels;

/// <summary>One row in the Players grid (§7.4), wrapping a <see cref="PlayerInfo"/>.</summary>
public partial class PlayerRowViewModel : ObservableObject
{
    public PlayerRowViewModel(PlayerInfo player) => Update(player);

    public PlayerInfo Player { get; private set; } = null!;

    public string Key => Player.Key;

    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private string _sinceText = string.Empty;
    [ObservableProperty] private bool _isOffline;
    [ObservableProperty] private string _platformGlyph = string.Empty;

    public void Update(PlayerInfo player)
    {
        Player = player;

        var name = string.IsNullOrWhiteSpace(player.PlayerName) ? FallbackName(player.PlayerId) : player.PlayerName;
        DisplayName = string.IsNullOrWhiteSpace(player.LastStatusCharacter)
            ? name
            : $"{name} ({player.LastStatusCharacter})";

        StatusText = player.PlayerStatus.ToString();
        IsOffline = player.PlayerStatus == PlayerStatus.Offline;
        PlatformGlyph = PlatformToIconConverter.ForPlatform(player.Platform);
        RefreshSince();
    }

    public void RefreshSince()
        => SinceText = RelativeTimeConverter.Format(Player.LastStatusChange, DateTimeOffset.Now);

    private static string FallbackName(string? playerId)
    {
        if (string.IsNullOrEmpty(playerId)) return "[unknown]";
        var last4 = playerId.Length <= 4 ? playerId : playerId[^4..];
        return $"[…{last4}]";
    }
}
