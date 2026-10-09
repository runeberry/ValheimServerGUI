using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using ValheimServerGUI.App.Tests.Fakes;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.App.ViewModels.Dialogs;
using ValheimServerGUI.Game;
using ValheimServerGUI.Localization;
using ValheimServerGUI.Tools.Models;
using Xunit;

namespace ValheimServerGUI.App.Tests.ViewModels;

public class PlayersViewModelTests
{
    private readonly ServerFormViewModel _form = new();
    private readonly FakeRuneberryApiClient _api = new();
    private readonly FakeUserPreferencesProvider _userPrefs = new();

    private PlayersViewModel NewVm(FakePlayerDataRepository repo) => new(repo, _form, _api, _userPrefs);

    // Saves the app-global defaults the way the Manage Players dialog does (raises PreferencesSaved).
    private void SaveDefaults(params (string key, PlayerRole role)[] entries)
    {
        var prefs = new UserPreferences();
        foreach (var (key, role) in entries)
            prefs.PlayerDefaults[key] = new PlayerDefaultEntry(role, "Steam");
        _userPrefs.SavePreferences(prefs);
    }

    private static PlayerInfo Player(string id, PlayerStatus status, string? name = null, string? character = null)
        => new()
        {
            Platform = "Steam",
            PlatformRaw = "Steam",
            PlayerId = id,
            PlayerName = name,
            LastStatusCharacter = character,
            PlayerStatus = status,
            LastStatusChange = DateTimeOffset.Now,
        };

    // ---- live table ----

    [AvaloniaFact]
    public void Entity_updated_adds_a_live_row()
    {
        var repo = new FakePlayerDataRepository();
        var vm = NewVm(repo);

        repo.PushUpdate(Player("76500001", PlayerStatus.Online, name: "Odin", character: "Ragnar"));

        var row = Assert.Single(vm.Players);
        Assert.Equal("Odin (Ragnar)", row.DisplayName);
        Assert.False(row.IsOffline);
    }

    [AvaloniaFact]
    public void Unnamed_player_falls_back_to_last4()
    {
        var repo = new FakePlayerDataRepository();
        var vm = NewVm(repo);

        repo.PushUpdate(Player("76500001234", PlayerStatus.Joining));

        Assert.Equal("[…1234]", Assert.Single(vm.Players).DisplayName);
    }

    [AvaloniaFact]
    public void Status_change_updates_existing_row_in_place()
    {
        var repo = new FakePlayerDataRepository();
        var vm = NewVm(repo);
        repo.PushUpdate(Player("1", PlayerStatus.Online, name: "A"));

        repo.RaiseStatusChanged(Player("1", PlayerStatus.Offline, name: "A"));

        var row = Assert.Single(vm.Players); // updated, not duplicated
        Assert.True(row.IsOffline);
    }

    [AvaloniaFact]
    public void View_details_enabled_only_with_a_selection()
    {
        var repo = new FakePlayerDataRepository();
        var vm = NewVm(repo);
        repo.PushUpdate(Player("1", PlayerStatus.Online, name: "A"));

        Assert.False(vm.CanViewDetails);
        vm.SelectedPlayer = vm.Players[0];
        Assert.True(vm.CanViewDetails);
        Assert.True(vm.ViewDetailsCommand.CanExecute(null));
    }

    [AvaloniaFact]
    public void Remove_enabled_only_for_offline_selection()
    {
        var repo = new FakePlayerDataRepository();
        var vm = NewVm(repo);
        repo.PushUpdate(Player("1", PlayerStatus.Online, name: "Online"));
        repo.PushUpdate(Player("2", PlayerStatus.Offline, name: "Offline"));

        vm.SelectedPlayer = vm.Players.First(r => r.Key == "Steam:1");
        Assert.False(vm.CanRemove);

        vm.SelectedPlayer = vm.Players.First(r => r.Key == "Steam:2");
        Assert.True(vm.CanRemove);
    }

    [AvaloniaFact]
    public void Remove_command_removes_from_repo()
    {
        var repo = new FakePlayerDataRepository();
        var vm = NewVm(repo);
        repo.PushUpdate(Player("2", PlayerStatus.Offline, name: "Offline"));
        vm.SelectedPlayer = vm.Players[0];

        vm.RemoveCommand.Execute(null);

        Assert.Empty(repo.Data);
        Assert.Empty(vm.Players);
    }

    // ---- mode-filtered role display ----

    [AvaloniaFact]
    public void Displayed_role_is_mode_filtered()
    {
        var repo = new FakePlayerDataRepository();
        var vm = NewVm(repo);
        vm.ShowBannedPlayers = true; // this test inspects the banned row
        var admin = Player("1", PlayerStatus.Offline);
        var permitted = Player("2", PlayerStatus.Offline);
        var banned = Player("3", PlayerStatus.Offline);
        _form.SetRole(admin, PlayerRole.Admin);
        _form.SetRole(permitted, PlayerRole.Permitted);
        _form.SetRole(banned, PlayerRole.Banned);
        repo.PushUpdate(admin);
        repo.PushUpdate(permitted);
        repo.PushUpdate(banned);

        // Open mode: admin + banned show; permitted is blank (its list is unused).
        _form.UsePermittedList = false;
        Assert.Equal(PlayerRole.Admin, RowFor(vm, "Steam:1").DisplayRole);
        Assert.Null(RowFor(vm, "Steam:2").DisplayRole);
        Assert.Equal(PlayerRole.Banned, RowFor(vm, "Steam:3").DisplayRole);

        // Permitted mode: admin + permitted show; banned is blank (the ban list is ignored).
        _form.UsePermittedList = true;
        Assert.Equal(PlayerRole.Admin, RowFor(vm, "Steam:1").DisplayRole);
        Assert.Equal(PlayerRole.Permitted, RowFor(vm, "Steam:2").DisplayRole);
        Assert.Null(RowFor(vm, "Steam:3").DisplayRole);
    }

    [AvaloniaFact]
    public void Role_text_and_icon_derive_from_displayed_role()
    {
        var row = new PlayerRowViewModel(Player("1", PlayerStatus.Offline)) { DisplayRole = PlayerRole.Admin };
        Assert.Equal(Strings.Role_Admin, row.RoleText);
        Assert.NotNull(row.RoleIcon);

        row.DisplayRole = null;
        Assert.Null(row.RoleText);
        Assert.Null(row.RoleIcon);
    }

    [AvaloniaFact]
    public void Since_is_blank_when_no_status_was_ever_recorded()
    {
        var row = new PlayerRowViewModel(new PlayerInfo { Platform = "Steam", PlayerId = "1" });
        Assert.Equal(string.Empty, row.SinceText);
    }

    [AvaloniaFact]
    public void Empty_table_shows_the_join_hint_until_a_player_appears()
    {
        var repo = new FakePlayerDataRepository();
        var vm = NewVm(repo);
        Assert.Equal(Strings.Players_EmptyText, vm.EmptyText);

        repo.PushUpdate(Player("1", PlayerStatus.Online));

        Assert.Null(vm.EmptyText);
    }

    // ---- banned-player view filter ----

    [AvaloniaFact]
    public void Players_banned_on_this_server_are_hidden_by_default_and_shown_on_request()
    {
        var repo = new FakePlayerDataRepository();
        SaveDefaults(("Steam:2", PlayerRole.Banned)); // globally banned
        var vm = NewVm(repo);
        var serverBanned = Player("3", PlayerStatus.Offline);
        _form.SetRole(serverBanned, PlayerRole.Banned);                      // banned on this server only
        repo.PushUpdate(Player("1", PlayerStatus.Offline));
        repo.PushUpdate(Player("2", PlayerStatus.Offline));
        repo.PushUpdate(serverBanned);
        repo.PushUpdate(Player("4", PlayerStatus.Offline));

        Assert.False(vm.ShowBannedPlayers);
        Assert.Equal(new[] { "Steam:1", "Steam:4" }, vm.Players.Select(r => r.Key));

        vm.ShowBannedPlayers = true;
        Assert.Equal(new[] { "Steam:1", "Steam:2", "Steam:3", "Steam:4" }, vm.Players.Select(r => r.Key)); // original order

        vm.ShowBannedPlayers = false;
        Assert.Equal(new[] { "Steam:1", "Steam:4" }, vm.Players.Select(r => r.Key));
    }

    [AvaloniaFact]
    public void A_server_override_that_unbans_a_globally_banned_player_shows_them()
    {
        var repo = new FakePlayerDataRepository();
        SaveDefaults(("Steam:1", PlayerRole.Banned));
        var vm = NewVm(repo);
        var player = Player("1", PlayerStatus.Offline);
        repo.PushUpdate(player);
        Assert.Empty(vm.Players);

        _form.SetRole(player, PlayerRole.Permitted);

        Assert.Equal("Steam:1", Assert.Single(vm.Players).Key);
    }

    [AvaloniaFact]
    public void Banning_the_selected_player_hides_the_row_and_clears_the_selection()
    {
        var repo = new FakePlayerDataRepository();
        var vm = NewVm(repo);
        repo.PushUpdate(Player("1", PlayerStatus.Offline));
        vm.SelectedPlayer = vm.Players[0];
        _form.UsePermittedList = false;

        vm.ServerRoleIsBanned = true;

        Assert.Equal(PlayerRole.Banned, _form.GetOverride("Steam:1")); // still tracked
        Assert.Empty(vm.Players);
        Assert.Null(vm.SelectedPlayer);

        vm.ShowBannedPlayers = true;
        Assert.Equal(Strings.Role_Banned, Assert.Single(vm.Players).RoleText);
    }

    // ---- "Set server role" submenu ----

    [AvaloniaFact]
    public void Server_role_submenu_sets_and_removes_the_override()
    {
        var repo = new FakePlayerDataRepository();
        var vm = NewVm(repo);
        repo.PushUpdate(Player("55", PlayerStatus.Offline));
        var row = vm.Players[0];
        vm.SelectedPlayer = row;
        Assert.True(vm.ServerRoleIsDefault); // no override yet

        vm.ServerRoleIsAdmin = true;
        Assert.Equal(PlayerRole.Admin, _form.GetOverride(row.Key));
        Assert.True(vm.ServerRoleIsAdmin);
        Assert.False(vm.ServerRoleIsDefault);
        Assert.True(_form.IsDirty); // profile working state

        vm.ServerRoleIsPermitted = true; // replaces the prior override (single role)
        Assert.Equal(PlayerRole.Permitted, _form.GetOverride(row.Key));

        vm.ServerRoleIsAdmin = false;    // a radio group's uncheck write is ignored
        Assert.Equal(PlayerRole.Permitted, _form.GetOverride(row.Key));

        vm.ServerRoleIsDefault = true;   // the fourth item removes the override
        Assert.Null(_form.GetOverride(row.Key));
        Assert.True(vm.ServerRoleIsDefault);
    }

    [AvaloniaFact]
    public void Fourth_item_names_the_default_it_falls_back_to_or_None()
    {
        var repo = new FakePlayerDataRepository();
        SaveDefaults(("Steam:1", PlayerRole.Admin));
        var vm = NewVm(repo);
        repo.PushUpdate(Player("1", PlayerStatus.Offline));
        repo.PushUpdate(Player("2", PlayerStatus.Offline));

        vm.SelectedPlayer = RowFor(vm, "Steam:1");
        Assert.Equal(string.Format(Strings.Players_Menu_DefaultRole, Strings.Role_Admin), vm.ServerRoleDefaultLabel);

        vm.SelectedPlayer = RowFor(vm, "Steam:2");
        Assert.Equal(Strings.Role_None, vm.ServerRoleDefaultLabel);

        SaveDefaults(("Steam:2", PlayerRole.Permitted)); // follows saved defaults
        Assert.Equal(string.Format(Strings.Players_Menu_DefaultRole, Strings.Role_Permitted), vm.ServerRoleDefaultLabel);
    }

    [AvaloniaFact]
    public void Removing_the_override_restores_the_default_role()
    {
        var repo = new FakePlayerDataRepository();
        SaveDefaults(("Steam:1", PlayerRole.Permitted));
        var vm = NewVm(repo);
        repo.PushUpdate(Player("1", PlayerStatus.Offline));
        _form.UsePermittedList = true;
        vm.SelectedPlayer = vm.Players[0];

        vm.ServerRoleIsAdmin = true;
        Assert.Equal(string.Format(Strings.Players_RoleOverridden, Strings.Role_Admin), RowFor(vm, "Steam:1").RoleText);

        vm.ServerRoleIsDefault = true;
        Assert.Equal(Strings.Role_Permitted, RowFor(vm, "Steam:1").RoleText);
    }

    // A server never stores a "no role" override: setting None just removes the override.
    [AvaloniaFact]
    public void A_None_server_role_is_never_stored()
    {
        var repo = new FakePlayerDataRepository();
        SaveDefaults(("Steam:1", PlayerRole.Admin));
        var vm = NewVm(repo);
        var player = Player("1", PlayerStatus.Offline);
        repo.PushUpdate(player);
        _form.SetRole(player, PlayerRole.Banned);

        _form.SetRole(player, PlayerRole.None);

        Assert.Null(_form.GetOverride("Steam:1"));
        Assert.Equal(Strings.Role_Admin, RowFor(vm, "Steam:1").RoleText);
    }

    // ---- global defaults + server overrides ----

    [AvaloniaFact]
    public void Default_role_shows_without_a_marker()
    {
        var repo = new FakePlayerDataRepository();
        SaveDefaults(("Steam:1", PlayerRole.Admin));
        var vm = NewVm(repo);
        repo.PushUpdate(Player("1", PlayerStatus.Offline));

        var row = RowFor(vm, "Steam:1");
        Assert.Equal(PlayerRole.Admin, row.DisplayRole);
        Assert.Equal(Strings.Role_Admin, row.RoleText);
    }

    [AvaloniaFact]
    public void Override_of_a_listed_player_is_marked_even_when_equal_to_the_default()
    {
        var repo = new FakePlayerDataRepository();
        SaveDefaults(("Steam:1", PlayerRole.Admin));
        var vm = NewVm(repo);
        var player = Player("1", PlayerStatus.Offline);
        repo.PushUpdate(player);

        _form.SetRole(player, PlayerRole.Admin); // pins the current default
        Assert.Equal(string.Format(Strings.Players_RoleOverridden, Strings.Role_Admin), RowFor(vm, "Steam:1").RoleText);
    }

    [AvaloniaFact]
    public void Override_of_an_unlisted_player_is_never_marked()
    {
        var repo = new FakePlayerDataRepository();
        var vm = NewVm(repo);
        var player = Player("1", PlayerStatus.Offline);
        repo.PushUpdate(player);

        _form.SetRole(player, PlayerRole.Admin);
        Assert.Equal(Strings.Role_Admin, RowFor(vm, "Steam:1").RoleText);
    }

    [AvaloniaFact]
    public void Saving_defaults_re_renders_rows()
    {
        var repo = new FakePlayerDataRepository();
        var vm = NewVm(repo);
        repo.PushUpdate(Player("1", PlayerStatus.Offline));
        Assert.Null(RowFor(vm, "Steam:1").RoleText);

        SaveDefaults(("Steam:1", PlayerRole.Admin));

        Assert.Equal(Strings.Role_Admin, RowFor(vm, "Steam:1").RoleText);
    }

    [AvaloniaFact]
    public void Manage_players_command_raises_the_request()
    {
        var vm = NewVm(new FakePlayerDataRepository());
        var raised = false;
        vm.ManagePlayersRequested += () => raised = true;

        vm.ManagePlayersCommand.Execute(null);

        Assert.True(raised);
    }

    [AvaloniaFact]
    public void Profile_load_re_renders_rows_for_the_new_roles()
    {
        var repo = new FakePlayerDataRepository();
        var vm = NewVm(repo);
        repo.PushUpdate(Player("321", PlayerStatus.Offline));
        Assert.Null(vm.Players[0].DisplayRole);

        // A profile load pushes new roles onto the form; the tab re-renders off RoleStateChanged.
        var prefs = new ServerPreferences { ProfileName = "P" };
        prefs.PlayerRoles["Steam:321"] = new PlayerRoleEntry(PlayerRole.Admin, "Steam");
        _form.LoadFieldsFrom(prefs);

        Assert.Equal(PlayerRole.Admin, vm.Players[0].DisplayRole);
    }

    // ---- add player ----

    [AvaloniaFact]
    public async Task Add_player_offers_the_server_preset_for_the_mode()
    {
        var vm = NewVm(new FakePlayerDataRepository());
        var offered = new System.Collections.Generic.List<AddPlayerOptions>();
        vm.AddPlayerPrompt = options => { offered.Add(options); return Task.FromResult<AddPlayerResult?>(null); };

        _form.UsePermittedList = false;
        await vm.AddPlayerCommand.ExecuteAsync(null);
        _form.UsePermittedList = true;
        await vm.AddPlayerCommand.ExecuteAsync(null);

        Assert.Equal(new[] { AddPlayerOptions.ForServer(false), AddPlayerOptions.ForServer(true) }, offered);
    }

    [AvaloniaFact]
    public async Task Add_player_with_a_server_role_stores_an_override()
    {
        var repo = new FakePlayerDataRepository();
        var vm = NewVm(repo);
        vm.AddPlayerPrompt = _ => Task.FromResult<AddPlayerResult?>(
            new AddPlayerResult(PlayerPlatforms.Xbox, "XUID9", "Thor", PlayerRole.Admin, AsDefault: false));

        await vm.AddPlayerCommand.ExecuteAsync(null);

        var row = Assert.Single(vm.Players);
        Assert.Equal("Xbox:XUID9", row.Key);
        Assert.Equal("Thor", row.Player.PlayerName);
        Assert.Equal(PlayerRole.Admin, _form.GetOverride(row.Key));
        Assert.Empty(_userPrefs.LoadPreferences().PlayerDefaults);
        Assert.Equal(0, _api.RequestPlayerInfoCallCount); // name given, no lookup needed
    }

    [AvaloniaFact]
    public async Task Add_player_as_default_saves_the_default_and_clears_this_servers_override()
    {
        var repo = new FakePlayerDataRepository();
        var existing = Player("5", PlayerStatus.Offline, name: "A");
        repo.PushUpdate(existing);
        var vm = NewVm(repo);
        _form.SetRole(existing, PlayerRole.Banned);
        var saves = _userPrefs.SaveCount;
        vm.AddPlayerPrompt = _ => Task.FromResult<AddPlayerResult?>(
            new AddPlayerResult(PlayerPlatforms.Steam, "5", null, PlayerRole.Admin, AsDefault: true));

        await vm.AddPlayerCommand.ExecuteAsync(null);

        Assert.Equal(saves + 1, _userPrefs.SaveCount);
        Assert.Equal(PlayerRole.Admin, _userPrefs.LoadPreferences().PlayerDefaults["Steam:5"].DefaultRole);
        Assert.Null(_form.GetOverride("Steam:5"));
        Assert.Equal(Strings.Role_Admin, RowFor(vm, "Steam:5").RoleText); // the default now applies here, unmarked
        Assert.Equal("A", RowFor(vm, "Steam:5").Player.PlayerName); // blank name keeps the cached one
    }

    [AvaloniaFact]
    public async Task Add_player_new_record_without_a_name_triggers_a_lookup()
    {
        var repo = new FakePlayerDataRepository();
        var vm = NewVm(repo);
        vm.AddPlayerPrompt = _ => Task.FromResult<AddPlayerResult?>(
            new AddPlayerResult(PlayerPlatforms.Steam, "77", null, PlayerRole.Admin, AsDefault: false));

        await vm.AddPlayerCommand.ExecuteAsync(null);

        Assert.Equal(1, _api.RequestPlayerInfoCallCount); // same lookup path a join uses
    }

    [AvaloniaFact]
    public async Task Add_player_cancelled_does_nothing()
    {
        var repo = new FakePlayerDataRepository();
        var vm = NewVm(repo);
        vm.AddPlayerPrompt = _ => Task.FromResult<AddPlayerResult?>(null);

        await vm.AddPlayerCommand.ExecuteAsync(null);

        Assert.Empty(vm.Players);
    }

    private static PlayerRowViewModel RowFor(PlayersViewModel vm, string key)
        => vm.Players.First(r => r.Key == key);
}
