using System.Linq;
using ValheimServerGUI.App.ViewModels.Dialogs;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools.Models;
using Xunit;

namespace ValheimServerGUI.App.Tests.ViewModels;

public class AddPlayerViewModelTests
{
    [Fact]
    public void Server_preset_offers_every_role_and_defaults_to_None()
    {
        var vm = new AddPlayerViewModel(AddPlayerOptions.ForServer);

        Assert.Equal(new[] { PlayerRole.Admin, PlayerRole.Permitted, PlayerRole.Banned, PlayerRole.None }, vm.Roles);
        Assert.Equal(PlayerRole.None, vm.SelectedRole);
        Assert.True(vm.ShowRole);
    }

    [Fact]
    public void My_accounts_preset_prefills_Admin_and_Friends_prefills_Permitted()
    {
        var mine = new AddPlayerViewModel(AddPlayerOptions.ForMyAccounts);
        var friends = new AddPlayerViewModel(AddPlayerOptions.ForFriends);

        Assert.Equal(PlayerRole.Admin, mine.SelectedRole);
        Assert.Equal(PlayerRole.Permitted, friends.SelectedRole);
        // Banned is its own list, not a role on the other lists.
        Assert.DoesNotContain(PlayerRole.Banned, mine.Roles);
        Assert.DoesNotContain(PlayerRole.Banned, friends.Roles);
    }

    [Fact]
    public void Banned_preset_hides_the_role_and_always_returns_Banned()
    {
        var vm = new AddPlayerViewModel(AddPlayerOptions.ForBanned) { PlayerId = "1" };

        Assert.False(vm.ShowRole);
        Assert.Equal(PlayerRole.Banned, vm.BuildResult()!.Role);
    }

    [Fact]
    public void CanSubmit_requires_a_platform_id()
    {
        var vm = new AddPlayerViewModel(AddPlayerOptions.ForServer);
        Assert.False(vm.CanSubmit);
        Assert.Null(vm.BuildResult());

        vm.PlayerId = "   ";
        Assert.False(vm.CanSubmit);

        vm.PlayerId = "123";
        Assert.True(vm.CanSubmit);
    }

    [Fact]
    public void Result_trims_fields_and_treats_a_blank_name_as_none()
    {
        var vm = new AddPlayerViewModel(AddPlayerOptions.ForFriends)
        {
            SelectedPlatform = PlayerPlatforms.Xbox,
            PlayerId = "  XUID ",
            PlayerName = "   ",
        };

        Assert.Equal(new AddPlayerResult(PlayerPlatforms.Xbox, "XUID", null, PlayerRole.Permitted), vm.BuildResult());

        vm.PlayerName = " Thor ";
        Assert.Equal("Thor", vm.BuildResult()!.PlayerName);
    }

    [Fact]
    public void Platforms_come_from_the_canonical_list_with_Steam_first()
    {
        var vm = new AddPlayerViewModel(AddPlayerOptions.ForServer);

        Assert.Equal(PlayerPlatforms.All, vm.Platforms.ToArray());
        Assert.Equal(PlayerPlatforms.Steam, vm.Platforms[0]);
        Assert.Equal(PlayerPlatforms.Steam, vm.SelectedPlatform);
    }
}
