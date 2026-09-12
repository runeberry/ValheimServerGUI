using System;
using System.Linq;
using Avalonia.Headless.XUnit;
using ValheimServerGUI.App.Tests.Fakes;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.Game;
using Xunit;

namespace ValheimServerGUI.App.Tests.ViewModels;

public class PlayersViewModelTests
{
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
        var vm = new PlayersViewModel(repo);

        repo.PushUpdate(Player("76500001", PlayerStatus.Online, name: "Odin", character: "Ragnar"));

        var row = Assert.Single(vm.Players);
        Assert.Equal("Odin (Ragnar)", row.DisplayName);
        Assert.False(row.IsOffline);
    }

    [AvaloniaFact]
    public void Unnamed_player_falls_back_to_last4()
    {
        var repo = new FakePlayerDataRepository();
        var vm = new PlayersViewModel(repo);

        repo.PushUpdate(Player("76500001234", PlayerStatus.Joining));

        Assert.Equal("[…1234]", Assert.Single(vm.Players).DisplayName);
    }

    [AvaloniaFact]
    public void Status_change_updates_existing_row_in_place()
    {
        var repo = new FakePlayerDataRepository();
        var vm = new PlayersViewModel(repo);
        repo.PushUpdate(Player("1", PlayerStatus.Online, name: "A"));

        repo.RaiseStatusChanged(Player("1", PlayerStatus.Offline, name: "A"));

        var row = Assert.Single(vm.Players); // updated, not duplicated
        Assert.True(row.IsOffline);
    }

    [AvaloniaFact]
    public void View_details_enabled_only_with_a_selection()
    {
        var repo = new FakePlayerDataRepository();
        var vm = new PlayersViewModel(repo);
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
        var vm = new PlayersViewModel(repo);
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
        var vm = new PlayersViewModel(repo);
        repo.PushUpdate(Player("2", PlayerStatus.Offline, name: "Offline"));
        vm.SelectedPlayer = vm.Players[0];

        vm.RemoveCommand.Execute(null);

        Assert.Empty(repo.Data);
        Assert.Empty(vm.Players);
    }
}
