using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools.Models;

namespace ValheimServerGUI.App.ViewModels.Dialogs;

/// <summary>What the Add Player dialog returns.</summary>
/// <param name="Role">The chosen role (None = no role).</param>
/// <param name="AsDefault">True to apply <paramref name="Role"/> as the player's default role on every server; false
/// to apply it to the current server only (a server override). Always false for None from the server dialog.</param>
public record AddPlayerResult(string Platform, string PlayerId, string? PlayerName, PlayerRole Role, bool AsDefault);

/// <summary>Which role the Add Player dialog sets.</summary>
public enum AddPlayerTarget
{
    /// <summary>The player's default role (Manage Players tables): the "Default Role" field.</summary>
    DefaultRole,

    /// <summary>The current server's role (Players tab): the "Server Role" field + "Set as default" checkbox.</summary>
    ServerRole,
}

/// <summary>How the Add Player dialog is opened: which role field it shows and which role it preselects.</summary>
public sealed record AddPlayerOptions(AddPlayerTarget Target, PlayerRole InitialRole)
{
    /// <summary>Manage Players → Player Accounts: Default Role, Permitted.</summary>
    public static AddPlayerOptions ForPlayerAccounts { get; } = new(AddPlayerTarget.DefaultRole, PlayerRole.Permitted);

    /// <summary>Manage Players → Banned: Default Role, Banned.</summary>
    public static AddPlayerOptions ForBanned { get; } = new(AddPlayerTarget.DefaultRole, PlayerRole.Banned);

    /// <summary>Players tab: Server Role, preselecting the role the server's mode makes meaningful.</summary>
    public static AddPlayerOptions ForServer(bool usePermittedList)
        => new(AddPlayerTarget.ServerRole, usePermittedList ? PlayerRole.Permitted : PlayerRole.None);
}

/// <summary>
/// Add Player dialog: a platform + platform ID (required), an optional player name, and a role. From Manage
/// Players the role is the player's <b>Default Role</b>; from the Players tab it is the <b>Server Role</b>, which the
/// "Set as default role" checkbox (on by default, unavailable for None) turns into the default role instead.
/// </summary>
public partial class AddPlayerViewModel : ObservableObject
{
    // ===== User copy (EXACT — do not paraphrase); asserted verbatim by a test. =====
    public const string DefaultRoleHelp = "Set the default role that this player will receive on all servers that you host.";
    public const string ServerRoleHelp = "Set the role for this player when they join this server.";
    public const string SetAsDefaultHelp =
        "Apply this role to this player for all servers that you host, unless a server-specific role is set as an override.";

    private readonly AddPlayerTarget _target;

    public AddPlayerViewModel(AddPlayerOptions options)
    {
        _target = options.Target;
        _selectedRole = options.InitialRole;
    }

    /// <summary>The platforms a manual entry can target.</summary>
    public IReadOnlyList<string> Platforms => PlayerPlatforms.All;

    [ObservableProperty] private string _selectedPlatform = PlayerPlatforms.Steam;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    private string _playerId = string.Empty;

    [ObservableProperty] private string _playerName = string.Empty;

    /// <summary>Every role, in menu order.</summary>
    public IReadOnlyList<PlayerRole> Roles { get; } =
        new[] { PlayerRole.Admin, PlayerRole.Permitted, PlayerRole.Banned, PlayerRole.None };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSetAsDefault))]
    private PlayerRole _selectedRole;

    public bool ShowDefaultRole => _target == AddPlayerTarget.DefaultRole;

    public bool ShowServerRole => _target == AddPlayerTarget.ServerRole;

    /// <summary>"Set as default role for this player" (server dialog only); checked by default.</summary>
    [ObservableProperty] private bool _setAsDefault = true;

    /// <summary>None has nothing to make a default of, so the checkbox is disabled for it.</summary>
    public bool CanSetAsDefault => SelectedRole != PlayerRole.None;

    /// <summary>Add Player is enabled once a platform ID is entered.</summary>
    public bool CanSubmit => !string.IsNullOrWhiteSpace(PlayerId);

    /// <summary>The dialog result, or null when the form is incomplete.</summary>
    public AddPlayerResult? BuildResult() => CanSubmit
        ? new AddPlayerResult(
            SelectedPlatform,
            PlayerId.Trim(),
            string.IsNullOrWhiteSpace(PlayerName) ? null : PlayerName.Trim(),
            SelectedRole,
            AsDefault: ShowDefaultRole || (SetAsDefault && CanSetAsDefault))
        : null;
}
