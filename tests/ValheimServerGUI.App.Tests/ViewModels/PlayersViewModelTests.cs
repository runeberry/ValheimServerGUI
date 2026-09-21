using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using ValheimServerGUI.App.Tests.Fakes;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools.Models;
using Xunit;

namespace ValheimServerGUI.App.Tests.ViewModels;

public class PlayersViewModelTests
{
    private readonly ServerFormViewModel _form = new();
    private readonly FakeRuneberryApiClient _api = new();

    private PlayersViewModel NewVm(FakePlayerDataRepository repo) => new(repo, _form, _api);

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
        Assert.Equal("Admin", row.RoleText);
        Assert.NotNull(row.RoleIcon);

        row.DisplayRole = null;
        Assert.Null(row.RoleText);
        Assert.Null(row.RoleIcon);
    }

    // ---- menu labels + mode-gated visibility ----

    [AvaloniaFact]
    public void Menu_labels_reflect_stored_role_and_toggle_verbs()
    {
        var repo = new FakePlayerDataRepository();
        var vm = NewVm(repo);
        var player = Player("1", PlayerStatus.Offline);
        repo.PushUpdate(player);
        vm.SelectedPlayer = vm.Players[0];

        Assert.Equal("Make admin", vm.AdminToggleLabel);
        _form.SetRole(player, PlayerRole.Admin);
        Assert.Equal("Remove admin", vm.AdminToggleLabel);
    }

    [AvaloniaFact]
    public void Ban_and_permit_verbs_are_gated_on_mode()
    {
        var repo = new FakePlayerDataRepository();
        var vm = NewVm(repo);

        _form.UsePermittedList = false;
        Assert.True(vm.ShowBanToggle);
        Assert.False(vm.ShowPermitToggle);

        _form.UsePermittedList = true;
        Assert.False(vm.ShowBanToggle);
        Assert.True(vm.ShowPermitToggle);
    }

    // ---- role transitions via the toggle commands ----

    [AvaloniaFact]
    public void Toggle_admin_sets_then_clears_the_role()
    {
        var repo = new FakePlayerDataRepository();
        var vm = NewVm(repo);
        repo.PushUpdate(Player("55", PlayerStatus.Offline));
        var row = vm.Players[0];
        vm.SelectedPlayer = row;

        vm.ToggleAdminCommand.Execute(null);
        Assert.Equal(PlayerRole.Admin, _form.GetRole(row.Key));
        Assert.Equal(PlayerRole.Admin, row.DisplayRole);
        Assert.Equal("Remove admin", vm.AdminToggleLabel);

        vm.ToggleAdminCommand.Execute(null); // negative verb clears to none
        Assert.Null(_form.GetRole(row.Key));
        Assert.Null(row.DisplayRole);
        Assert.Equal("Make admin", vm.AdminToggleLabel);
    }

    [AvaloniaFact]
    public void Setting_a_new_role_replaces_the_prior_one()
    {
        var repo = new FakePlayerDataRepository();
        var vm = NewVm(repo);
        var player = Player("7", PlayerStatus.Offline);
        repo.PushUpdate(player);
        var row = vm.Players[0];
        vm.SelectedPlayer = row;
        _form.UsePermittedList = false;

        vm.ToggleAdminCommand.Execute(null);
        vm.ToggleBanCommand.Execute(null); // ban replaces admin (single role)

        Assert.Equal(PlayerRole.Banned, _form.GetRole(row.Key));
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

    // ---- add by id ----

    [AvaloniaFact]
    public async Task Add_by_id_creates_a_row_and_stores_a_role()
    {
        var repo = new FakePlayerDataRepository();
        var vm = NewVm(repo);
        _form.UsePermittedList = false;
        vm.AddByIdPrompt = () => Task.FromResult<AddByIdResult?>(
            new AddByIdResult(PlayerPlatforms.Xbox, "XUID9", Admin: true, Banned: false, Permitted: false));

        await vm.AddByIdCommand.ExecuteAsync(null);

        var row = Assert.Single(vm.Players);
        Assert.Equal("Xbox:XUID9", row.Key);
        Assert.Equal(PlayerRole.Admin, _form.GetRole(row.Key));
        Assert.Equal(PlayerRole.Admin, row.DisplayRole);
    }

    [AvaloniaFact]
    public async Task Add_by_id_collapses_multi_select_to_a_single_role_ban_wins()
    {
        var repo = new FakePlayerDataRepository();
        var vm = NewVm(repo);
        vm.AddByIdPrompt = () => Task.FromResult<AddByIdResult?>(
            new AddByIdResult(PlayerPlatforms.Steam, "42", Admin: true, Banned: true, Permitted: false));

        await vm.AddByIdCommand.ExecuteAsync(null);

        Assert.Equal(PlayerRole.Banned, _form.GetRole("Steam:42")); // ban wins over admin
    }

    [AvaloniaFact]
    public async Task Add_by_id_new_record_triggers_a_name_lookup()
    {
        var repo = new FakePlayerDataRepository();
        var vm = NewVm(repo);
        vm.AddByIdPrompt = () => Task.FromResult<AddByIdResult?>(
            new AddByIdResult(PlayerPlatforms.Steam, "77", Admin: false, Banned: false, Permitted: false));

        await vm.AddByIdCommand.ExecuteAsync(null);

        Assert.Equal(1, _api.RequestPlayerInfoCallCount); // same lookup path a join uses
    }

    [AvaloniaFact]
    public async Task Add_by_id_cancelled_does_nothing()
    {
        var repo = new FakePlayerDataRepository();
        var vm = NewVm(repo);
        vm.AddByIdPrompt = () => Task.FromResult<AddByIdResult?>(null);

        await vm.AddByIdCommand.ExecuteAsync(null);

        Assert.Empty(vm.Players);
    }

    private static PlayerRowViewModel RowFor(PlayersViewModel vm, string key)
        => vm.Players.First(r => r.Key == key);
}
