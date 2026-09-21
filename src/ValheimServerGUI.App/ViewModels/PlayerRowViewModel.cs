using System;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using ValheimServerGUI.App.Controls;
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
    [ObservableProperty] private PlayerStatus _status;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private string _sinceText = string.Empty;
    [ObservableProperty] private bool _isOffline;
    [ObservableProperty] private Bitmap? _platformIcon;

    // The role shown for the current profile + mode, computed by PlayersViewModel (mode-filtered from the
    // stored role: admin shows in both modes; permitted only in permitted-list mode; banned only otherwise).
    // Null renders a blank cell. The stored role itself lives on the profile (ServerFormViewModel), not here.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RoleText), nameof(RoleIcon))]
    private PlayerRole? _displayRole;

    /// <summary>The displayed role's label, or null (blank cell) when the player has no role in this mode.</summary>
    public string? RoleText => DisplayRole switch
    {
        PlayerRole.Admin => "Admin",
        PlayerRole.Permitted => "Permitted",
        PlayerRole.Banned => "Banned",
        _ => null,
    };

    /// <summary>Icon for <see cref="RoleText"/> (null when the player has no role in this mode).</summary>
    public Bitmap? RoleIcon => DisplayRole switch
    {
        PlayerRole.Admin => AppIcons.Get("UserAdmin_16x"),
        PlayerRole.Permitted => AppIcons.Get("UserOk_16x"),
        PlayerRole.Banned => AppIcons.Get("InUseByOtherUser_16x"),
        _ => null,
    };

    public void Update(PlayerInfo player)
    {
        Player = player;

        var name = string.IsNullOrWhiteSpace(player.PlayerName) ? FallbackName(player.PlayerId) : player.PlayerName;
        DisplayName = string.IsNullOrWhiteSpace(player.LastStatusCharacter)
            ? name
            : $"{name} ({player.LastStatusCharacter})";

        Status = player.PlayerStatus;
        StatusText = player.PlayerStatus.ToString();
        IsOffline = player.PlayerStatus == PlayerStatus.Offline;
        PlatformIcon = PlatformToIconConverter.ForPlatform(player.Platform);
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
