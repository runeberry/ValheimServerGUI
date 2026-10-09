using System.Linq;
using ValheimServerGUI.App.ViewModels.Dialogs;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools.Models;
using Xunit;

namespace ValheimServerGUI.App.Tests.ViewModels;

public class AddPlayerViewModelTests
{
    [Fact]
    public void Manage_players_presets_show_the_default_role_field()
    {
        var accounts = new AddPlayerViewModel(AddPlayerOptions.ForPlayerAccounts);
        var banned = new AddPlayerViewModel(AddPlayerOptions.ForBanned);

        Assert.True(accounts.ShowDefaultRole);
        Assert.False(accounts.ShowServerRole);
        Assert.Equal(PlayerRole.Permitted, accounts.SelectedRole);
        Assert.Equal(PlayerRole.Banned, banned.SelectedRole);
    }

    [Fact]
    public void Server_preset_shows_the_server_role_field_and_follows_the_mode()
    {
        var open = new AddPlayerViewModel(AddPlayerOptions.ForServer(usePermittedList: false));
        var permitted = new AddPlayerViewModel(AddPlayerOptions.ForServer(usePermittedList: true));

        Assert.True(open.ShowServerRole);
        Assert.False(open.ShowDefaultRole);
        Assert.Equal(PlayerRole.None, open.SelectedRole);
        Assert.Equal(PlayerRole.Permitted, permitted.SelectedRole);
    }

    [Fact]
    public void Every_preset_offers_every_role()
    {
        var expected = new[] { PlayerRole.Admin, PlayerRole.Permitted, PlayerRole.Banned, PlayerRole.None };
        Assert.Equal(expected, new AddPlayerViewModel(AddPlayerOptions.ForBanned).Roles);
        Assert.Equal(expected, new AddPlayerViewModel(AddPlayerOptions.ForServer(false)).Roles);
    }

    [Fact]
    public void Set_as_default_is_on_by_default_and_unavailable_for_None()
    {
        var vm = new AddPlayerViewModel(AddPlayerOptions.ForServer(usePermittedList: true)) { PlayerId = "1" };
        Assert.True(vm.SetAsDefault);
        Assert.True(vm.CanSetAsDefault);
        Assert.True(vm.BuildResult()!.AsDefault);

        vm.SetAsDefault = false;
        Assert.False(vm.BuildResult()!.AsDefault);

        vm.SetAsDefault = true;
        vm.SelectedRole = PlayerRole.None;
        Assert.False(vm.CanSetAsDefault);
        Assert.False(vm.BuildResult()!.AsDefault); // a disabled checkbox never applies
    }

    [Fact]
    public void Default_role_field_always_applies_as_a_default()
    {
        var vm = new AddPlayerViewModel(AddPlayerOptions.ForPlayerAccounts) { PlayerId = "1", SetAsDefault = false };
        Assert.True(vm.BuildResult()!.AsDefault);
    }

    [Fact]
    public void CanSubmit_requires_a_platform_id()
    {
        var vm = new AddPlayerViewModel(AddPlayerOptions.ForPlayerAccounts);
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
        var vm = new AddPlayerViewModel(AddPlayerOptions.ForPlayerAccounts)
        {
            SelectedPlatform = PlayerPlatforms.Xbox,
            PlayerId = "  XUID ",
            PlayerName = "   ",
        };

        Assert.Equal(new AddPlayerResult(PlayerPlatforms.Xbox, "XUID", null, PlayerRole.Permitted, true), vm.BuildResult());

        vm.PlayerName = " Thor ";
        Assert.Equal("Thor", vm.BuildResult()!.PlayerName);
    }

    [Fact]
    public void Platforms_come_from_the_canonical_list_with_Steam_first()
    {
        var vm = new AddPlayerViewModel(AddPlayerOptions.ForPlayerAccounts);

        Assert.Equal(PlayerPlatforms.All, vm.Platforms.ToArray());
        Assert.Equal(PlayerPlatforms.Steam, vm.SelectedPlatform);
    }
}
