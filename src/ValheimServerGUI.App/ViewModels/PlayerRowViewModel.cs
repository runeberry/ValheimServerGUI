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

    // Access-list membership (profile-scoped), driven by PlayersViewModel from the list files. A glyph shows
    // only when the player is a member; the column header supplies the meaning.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AdminIcon))]
    private bool _isAdmin;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BannedIcon))]
    private bool _isBanned;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PermittedIcon))]
    private bool _isPermitted;

    public Bitmap? AdminIcon => IsAdmin ? AppIcons.Get("StatusOK_16x") : null;
    public Bitmap? BannedIcon => IsBanned ? AppIcons.Get("Cancel_16x") : null;
    public Bitmap? PermittedIcon => IsPermitted ? AppIcons.Get("StatusOnline_16x") : null;

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

    /// <summary>Sets the three access-list flags together (called by <c>PlayersViewModel</c> on load/toggle).</summary>
    public void SetMembership(bool isAdmin, bool isBanned, bool isPermitted)
    {
        IsAdmin = isAdmin;
        IsBanned = isBanned;
        IsPermitted = isPermitted;
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
