using System;
using System.Linq;
using Avalonia.Headless.XUnit;
using ValheimServerGUI.App.Converters;
using ValheimServerGUI.App.Tests.Fakes;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.Game;
using ValheimServerGUI.Localization;
using Xunit;

namespace ValheimServerGUI.App.Tests.ViewModels;

/// <summary>
/// The Players tab's role truth table (Nuffle's spec): for every combination of the player's default role, this
/// server's role, the permitted-list mode, and the "Show banned players" filter, whether the row is visible and
/// what its Role column reads. Driven end to end through <see cref="PlayersViewModel"/>: the default comes from
/// user preferences, the server role is the profile override on the form.
/// </summary>
public class PlayerRoleTruthTableTests
{
    // null default = the player has no default role; null server role = no server-specific role.
    // Expected role "" = the Role column is blank; null = not checked (the row is hidden). The role is shorthand
    // ("Admin", "Admin (*)" for an override marker), resolved to the displayed copy by ExpectedRoleText.
    public static readonly TheoryData<PlayerRole?, PlayerRole?, bool, bool, bool, string?> Cases = new()
    {
        // default              server role           permit  showBan visible role
        { PlayerRole.Admin,     PlayerRole.Admin,     true,  true,  true,  "Admin (*)" },
        { PlayerRole.Admin,     PlayerRole.Admin,     true,  false, true,  "Admin (*)" },
        { PlayerRole.Admin,     PlayerRole.Admin,     false, true,  true,  "Admin (*)" },
        { PlayerRole.Admin,     PlayerRole.Admin,     false, false, true,  "Admin (*)" },
        { PlayerRole.Admin,     PlayerRole.Permitted, true,  true,  true,  "Permitted (*)" },
        { PlayerRole.Admin,     PlayerRole.Permitted, true,  false, true,  "Permitted (*)" },
        { PlayerRole.Admin,     PlayerRole.Permitted, false, true,  true,  "" },
        { PlayerRole.Admin,     PlayerRole.Permitted, false, false, true,  "" },
        { PlayerRole.Admin,     PlayerRole.Banned,    true,  true,  true,  "" },
        { PlayerRole.Admin,     PlayerRole.Banned,    true,  false, false, null },
        { PlayerRole.Admin,     PlayerRole.Banned,    false, true,  true,  "Banned (*)" },
        { PlayerRole.Admin,     PlayerRole.Banned,    false, false, false, null },
        { PlayerRole.Admin,     null,                 true,  true,  true,  "Admin" },
        { PlayerRole.Admin,     null,                 true,  false, true,  "Admin" },
        { PlayerRole.Admin,     null,                 false, true,  true,  "Admin" },
        { PlayerRole.Admin,     null,                 false, false, true,  "Admin" },
        { PlayerRole.Permitted, PlayerRole.Admin,     true,  true,  true,  "Admin (*)" },
        { PlayerRole.Permitted, PlayerRole.Admin,     true,  false, true,  "Admin (*)" },
        { PlayerRole.Permitted, PlayerRole.Admin,     false, true,  true,  "Admin (*)" },
        { PlayerRole.Permitted, PlayerRole.Admin,     false, false, true,  "Admin (*)" },
        { PlayerRole.Permitted, PlayerRole.Permitted, true,  true,  true,  "Permitted (*)" },
        { PlayerRole.Permitted, PlayerRole.Permitted, true,  false, true,  "Permitted (*)" },
        { PlayerRole.Permitted, PlayerRole.Permitted, false, true,  true,  "" },
        { PlayerRole.Permitted, PlayerRole.Permitted, false, false, true,  "" },
        { PlayerRole.Permitted, PlayerRole.Banned,    true,  true,  true,  "" },
        { PlayerRole.Permitted, PlayerRole.Banned,    true,  false, false, null },
        { PlayerRole.Permitted, PlayerRole.Banned,    false, true,  true,  "Banned (*)" },
        { PlayerRole.Permitted, PlayerRole.Banned,    false, false, false, null },
        { PlayerRole.Permitted, null,                 true,  true,  true,  "Permitted" },
        { PlayerRole.Permitted, null,                 true,  false, true,  "Permitted" },
        { PlayerRole.Permitted, null,                 false, true,  true,  "" },
        { PlayerRole.Permitted, null,                 false, false, true,  "" },
        { PlayerRole.Banned,    PlayerRole.Admin,     true,  true,  true,  "Admin (*)" },
        { PlayerRole.Banned,    PlayerRole.Admin,     true,  false, true,  "Admin (*)" },
        { PlayerRole.Banned,    PlayerRole.Admin,     false, true,  true,  "Admin (*)" },
        { PlayerRole.Banned,    PlayerRole.Admin,     false, false, true,  "Admin (*)" },
        { PlayerRole.Banned,    PlayerRole.Permitted, true,  true,  true,  "Permitted (*)" },
        { PlayerRole.Banned,    PlayerRole.Permitted, true,  false, true,  "Permitted (*)" },
        { PlayerRole.Banned,    PlayerRole.Permitted, false, true,  true,  "" },
        { PlayerRole.Banned,    PlayerRole.Permitted, false, false, true,  "" },
        { PlayerRole.Banned,    PlayerRole.Banned,    true,  true,  true,  "" },
        { PlayerRole.Banned,    PlayerRole.Banned,    true,  false, false, null },
        { PlayerRole.Banned,    PlayerRole.Banned,    false, true,  true,  "Banned (*)" },
        { PlayerRole.Banned,    PlayerRole.Banned,    false, false, false, null },
        { PlayerRole.Banned,    null,                 true,  true,  true,  "" },
        { PlayerRole.Banned,    null,                 true,  false, false, null },
        { PlayerRole.Banned,    null,                 false, true,  true,  "Banned" },
        { PlayerRole.Banned,    null,                 false, false, false, null },
        { null,                 PlayerRole.Admin,     true,  true,  true,  "Admin" },
        { null,                 PlayerRole.Admin,     true,  false, true,  "Admin" },
        { null,                 PlayerRole.Admin,     false, true,  true,  "Admin" },
        { null,                 PlayerRole.Admin,     false, false, true,  "Admin" },
        { null,                 PlayerRole.Permitted, true,  true,  true,  "Permitted" },
        { null,                 PlayerRole.Permitted, true,  false, true,  "Permitted" },
        { null,                 PlayerRole.Permitted, false, true,  true,  "" },
        { null,                 PlayerRole.Permitted, false, false, true,  "" },
        { null,                 PlayerRole.Banned,    true,  true,  true,  "" },
        { null,                 PlayerRole.Banned,    true,  false, false, null },
        { null,                 PlayerRole.Banned,    false, true,  true,  "Banned" },
        { null,                 PlayerRole.Banned,    false, false, false, null },
        { null,                 null,                 true,  true,  true,  "" },
        { null,                 null,                 true,  false, true,  "" },
        { null,                 null,                 false, true,  true,  "" },
        { null,                 null,                 false, false, true,  "" },
    };

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void Players_tab_row_matches_the_truth_table(
        PlayerRole? defaultRole, PlayerRole? serverRole, bool usePermittedList, bool showBanned,
        bool expectVisible, string? expectRole)
    {
        var player = new PlayerInfo
        {
            Platform = "Steam", PlatformRaw = "Steam", PlayerId = "1", PlayerName = "Odin",
            PlayerStatus = PlayerStatus.Offline, LastStatusChange = DateTimeOffset.Now,
        };
        var prefs = new UserPreferences();
        if (defaultRole is { } d) prefs.PlayerDefaults[player.Key] = new PlayerDefaultEntry(d, "Steam");

        var repo = new FakePlayerDataRepository();
        repo.PushUpdate(player);
        var form = new ServerFormViewModel();
        var vm = new PlayersViewModel(repo, form, new FakeUserPreferencesProvider(prefs));

        if (serverRole is { } s) form.SetRole(player, s);
        form.UsePermittedList = usePermittedList;
        vm.ShowBannedPlayers = showBanned;

        var row = vm.Players.SingleOrDefault(r => r.Key == player.Key);
        Assert.Equal(expectVisible, row is not null);
        if (row is not null) Assert.Equal(ExpectedRoleText(expectRole!), row.RoleText ?? string.Empty);
    }

    private static string ExpectedRoleText(string shorthand)
    {
        if (shorthand.Length == 0) return string.Empty;
        var marked = shorthand.EndsWith(" (*)", StringComparison.Ordinal);
        var label = EnumDisplayConverter.ToText(Enum.Parse<PlayerRole>(marked ? shorthand[..^4] : shorthand));
        return marked ? string.Format(Strings.Players_RoleOverridden, label) : label;
    }

    // Guards the table itself: exactly one row per combination of the four inputs (4 × 4 × 2 × 2).
    [Fact]
    public void Truth_table_covers_every_combination_once()
    {
        var keys = Cases.Select(c => (c.Data.Item1, c.Data.Item2, c.Data.Item3, c.Data.Item4)).ToList();
        Assert.Equal(64, keys.Count);
        Assert.Equal(64, keys.Distinct().Count());
    }
}
