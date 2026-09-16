using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using ValheimServerGUI.App.Tests.Fakes;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools.Models;
using Xunit;

namespace ValheimServerGUI.App.Tests.ViewModels;

public class PlayersViewModelTests : IDisposable
{
    private readonly PlayerAccessListService _accessLists = new();
    private readonly DirectoryInfo _savedir =
        new(Path.Join(Path.GetTempPath(), "vsg_pvm_" + Guid.NewGuid().ToString("N")));

    public PlayersViewModelTests() => _savedir.Create();

    public void Dispose()
    {
        if (_savedir.Exists) _savedir.Delete(true);
    }

    private PlayersViewModel NewVm(FakePlayerDataRepository repo) => new(repo, _accessLists);

    private static PlayerInfo Player(string id, PlayerStatus status, string? name = null, string? character = null)
        => new()
        {
            Platform = "Steam",
            PlayerId = id,
            PlayerName = name,
            LastStatusCharacter = character,
            PlayerStatus = status,
            LastStatusChange = DateTimeOffset.Now,
        };

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

    // ---- access-list management ----

    [AvaloniaFact]
    public void Access_toggles_gated_on_savedir_and_selection()
    {
        var repo = new FakePlayerDataRepository();
        var vm = NewVm(repo);
        repo.PushUpdate(Player("1", PlayerStatus.Offline, name: "A"));

        Assert.False(vm.CanManageAccess);               // no selection, no savedir
        vm.SelectedPlayer = vm.Players[0];
        Assert.False(vm.CanManageAccess);               // selection but still no savedir

        vm.SetSaveDataFolder(_savedir.FullName);
        Assert.True(vm.CanManageAccess);
        Assert.True(vm.ToggleAdminCommand.CanExecute(null));
    }

    [AvaloniaFact]
    public void Toggle_admin_adds_then_removes_file_entry_and_flips_row()
    {
        var repo = new FakePlayerDataRepository();
        var vm = NewVm(repo);
        repo.PushUpdate(Player("55", PlayerStatus.Offline, name: "A"));
        vm.SetSaveDataFolder(_savedir.FullName);
        var row = vm.Players[0];
        vm.SelectedPlayer = row;

        vm.ToggleAdminCommand.Execute(null);
        Assert.True(row.IsAdmin);
        Assert.True(_accessLists.Contains(_savedir.FullName, PlayerAccessList.Admin, row.Player));
        Assert.Equal("Remove admin", vm.AdminToggleLabel);

        vm.ToggleAdminCommand.Execute(null);
        Assert.False(row.IsAdmin);
        Assert.False(_accessLists.Contains(_savedir.FullName, PlayerAccessList.Admin, row.Player));
        Assert.Equal("Make admin", vm.AdminToggleLabel);
    }

    [AvaloniaFact]
    public void Banning_an_admin_reports_the_override_notice()
    {
        var repo = new FakePlayerDataRepository();
        var vm = NewVm(repo);
        string? notice = null;
        vm.NoticeReported = msg => notice = msg;

        repo.PushUpdate(Player("7", PlayerStatus.Offline, name: "A"));
        vm.SetSaveDataFolder(_savedir.FullName);
        var row = vm.Players[0];
        vm.SelectedPlayer = row;

        vm.ToggleAdminCommand.Execute(null);
        vm.ToggleBanCommand.Execute(null);

        Assert.True(row.IsBanned);
        Assert.NotNull(notice);
        Assert.Contains("overrides", notice);
    }

    [AvaloniaFact]
    public void Retarget_reloads_membership_for_the_new_savedir()
    {
        var repo = new FakePlayerDataRepository();
        var vm = NewVm(repo);
        var player = Player("321", PlayerStatus.Offline, name: "A");
        repo.PushUpdate(player);

        // Pre-seed the admin list in the first savedir with this player.
        _accessLists.Add(_savedir.FullName, PlayerAccessList.Admin, player);
        vm.SetSaveDataFolder(_savedir.FullName);
        Assert.True(vm.Players[0].IsAdmin);

        // Switching to an empty savedir clears the membership glyphs.
        var other = new DirectoryInfo(Path.Join(Path.GetTempPath(), "vsg_pvm_" + Guid.NewGuid().ToString("N")));
        other.Create();
        try
        {
            vm.SetSaveDataFolder(other.FullName);
            Assert.False(vm.Players[0].IsAdmin);
        }
        finally
        {
            other.Delete(true);
        }
    }

    [AvaloniaFact]
    public async Task Add_by_id_creates_a_row_and_writes_the_selected_lists()
    {
        var repo = new FakePlayerDataRepository();
        var vm = NewVm(repo);
        vm.SetSaveDataFolder(_savedir.FullName);
        vm.AddByIdPrompt = () => Task.FromResult<AddByIdResult?>(
            new AddByIdResult(PlayerPlatforms.Xbox, "XUID9", Admin: true, Banned: false, Permitted: true));

        await vm.AddByIdCommand.ExecuteAsync(null);

        var row = Assert.Single(vm.Players);
        Assert.Equal("Xbox:XUID9", row.Key);
        Assert.True(row.IsAdmin);
        Assert.True(row.IsPermitted);
        Assert.False(row.IsBanned);
        Assert.True(_accessLists.Contains(_savedir.FullName, PlayerAccessList.Admin, row.Player));
        Assert.True(_accessLists.Contains(_savedir.FullName, PlayerAccessList.Permitted, row.Player));
    }

    [AvaloniaFact]
    public async Task Add_by_id_cancelled_does_nothing()
    {
        var repo = new FakePlayerDataRepository();
        var vm = NewVm(repo);
        vm.SetSaveDataFolder(_savedir.FullName);
        vm.AddByIdPrompt = () => Task.FromResult<AddByIdResult?>(null);

        await vm.AddByIdCommand.ExecuteAsync(null);

        Assert.Empty(vm.Players);
    }
}
