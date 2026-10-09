using System;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using ValheimServerGUI.App.Controls;
using ValheimServerGUI.App.Converters;
using ValheimServerGUI.Game;
using ValheimServerGUI.Localization;

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

    /// <summary>The account name alone (no character), or null when unknown. Used by the Manage Players lists,
    /// which show characters in their own table.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAccountName))]
    private string? _accountName;

    [ObservableProperty] private string _platformId = string.Empty;

    public bool HasAccountName => AccountName is not null;

    // The role shown in this row, computed by the owning list: on the Players tab the effective role for the
    // current profile + mode (admin shows in both modes; permitted only in permitted-list mode; banned only
    // otherwise); on a Manage Players list the account's default role. Null renders a blank cell.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RoleText), nameof(RoleIcon))]
    private PlayerRole? _displayRole;

    /// <summary>True when a server override replaces the player's global default (appends <c>(*)</c>).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RoleText))]
    private bool _showsOverrideMarker;

    /// <summary>The displayed role's label (with <c>(*)</c> when overridden), or null for a blank cell (no role).</summary>
    public string? RoleText
    {
        get
        {
            var label = DisplayRole is PlayerRole.Admin or PlayerRole.Permitted or PlayerRole.Banned
                ? EnumDisplayConverter.ToText(DisplayRole.Value)
                : null;
            return label is not null && ShowsOverrideMarker ? string.Format(Strings.Players_RoleOverridden, label) : label;
        }
    }

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
        AccountName = string.IsNullOrWhiteSpace(player.PlayerName) ? null : player.PlayerName;
        PlatformId = player.PlayerId ?? string.Empty;
        DisplayName = string.IsNullOrWhiteSpace(player.LastStatusCharacter)
            ? name
            : $"{name} ({player.LastStatusCharacter})";

        Status = player.PlayerStatus;
        StatusText = EnumDisplayConverter.ToText(player.PlayerStatus);
        IsOffline = player.PlayerStatus == PlayerStatus.Offline;
        PlatformIcon = PlatformToIconConverter.ForPlatform(player.Platform);
        RefreshSince();
    }

    // Blank when the status was never recorded (e.g. a Manage Players account that has never joined).
    public void RefreshSince()
        => SinceText = Player.LastStatusChange == default
            ? string.Empty
            : RelativeTimeConverter.Format(Player.LastStatusChange, DateTimeOffset.Now);

    /// <summary>The placeholder shown for a player with no known name: the last four ID characters.</summary>
    public static string FallbackName(string? playerId)
    {
        if (string.IsNullOrEmpty(playerId)) return Strings.Players_UnknownName;
        var last4 = playerId.Length <= 4 ? playerId : playerId[^4..];
        return $"[…{last4}]";
    }
}
