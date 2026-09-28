using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools.Models;

namespace ValheimServerGUI.App.ViewModels.Dialogs;

/// <summary>The identity, optional name, and role returned by the Add Player dialog.</summary>
public record AddPlayerResult(string Platform, string PlayerId, string? PlayerName, PlayerRole Role);

/// <summary>
/// Which roles the Add Player dialog offers, which one is preselected, and whether the Role field shows at all.
/// One preset per surface that opens the dialog.
/// </summary>
public sealed record AddPlayerOptions(IReadOnlyList<PlayerRole> Roles, PlayerRole DefaultRole, bool ShowRole)
{
    private static readonly PlayerRole[] ListRoles = { PlayerRole.Admin, PlayerRole.Permitted, PlayerRole.None };

    /// <summary>Players tab: sets a server override (None = clear any override).</summary>
    public static AddPlayerOptions ForServer { get; } = new(
        new[] { PlayerRole.Admin, PlayerRole.Permitted, PlayerRole.Banned, PlayerRole.None }, PlayerRole.None, true);

    /// <summary>Manage Players → My Accounts: default role prefilled to Admin.</summary>
    public static AddPlayerOptions ForMyAccounts { get; } = new(ListRoles, PlayerRole.Admin, true);

    /// <summary>Manage Players → Friends: default role prefilled to Permitted.</summary>
    public static AddPlayerOptions ForFriends { get; } = new(ListRoles, PlayerRole.Permitted, true);

    /// <summary>Manage Players → Banned: the role is always Banned, so the field is hidden.</summary>
    public static AddPlayerOptions ForBanned { get; } = new(new[] { PlayerRole.Banned }, PlayerRole.Banned, false);
}

/// <summary>
/// Add Player dialog: a platform + platform ID (required), an optional player name, and a role chosen from the
/// options the opening surface allows (<see cref="AddPlayerOptions"/>).
/// </summary>
public partial class AddPlayerViewModel : ObservableObject
{
    public AddPlayerViewModel(AddPlayerOptions options)
    {
        Roles = options.Roles;
        ShowRole = options.ShowRole;
        _selectedRole = options.DefaultRole;
    }

    /// <summary>The platforms a manual entry can target.</summary>
    public IReadOnlyList<string> Platforms => PlayerPlatforms.All;

    [ObservableProperty] private string _selectedPlatform = PlayerPlatforms.Steam;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    private string _playerId = string.Empty;

    [ObservableProperty] private string _playerName = string.Empty;

    public IReadOnlyList<PlayerRole> Roles { get; }

    public bool ShowRole { get; }

    [ObservableProperty] private PlayerRole _selectedRole;

    /// <summary>Add Player is enabled once a platform ID is entered.</summary>
    public bool CanSubmit => !string.IsNullOrWhiteSpace(PlayerId);

    /// <summary>The dialog result, or null when the form is incomplete.</summary>
    public AddPlayerResult? BuildResult() => CanSubmit
        ? new AddPlayerResult(
            SelectedPlatform,
            PlayerId.Trim(),
            string.IsNullOrWhiteSpace(PlayerName) ? null : PlayerName.Trim(),
            SelectedRole)
        : null;
}
