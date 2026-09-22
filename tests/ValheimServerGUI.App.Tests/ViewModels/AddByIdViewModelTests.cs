using ValheimServerGUI.App.ViewModels.Dialogs;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools.Models;
using Xunit;

namespace ValheimServerGUI.App.Tests.ViewModels;

public class AddByIdViewModelTests
{
    [Fact]
    public void Role_options_follow_the_mode()
    {
        var open = new AddByIdViewModel(usePermittedList: false);
        Assert.True(open.ShowBanned);       // ban list is in effect
        Assert.False(open.ShowPermitted);

        var permitted = new AddByIdViewModel(usePermittedList: true);
        Assert.False(permitted.ShowBanned);
        Assert.True(permitted.ShowPermitted); // whitelist is in effect
    }

    [Fact]
    public void CanSubmit_requires_an_id_and_a_role()
    {
        var vm = new AddByIdViewModel(usePermittedList: false);
        Assert.False(vm.CanSubmit);

        vm.PlayerId = "123";
        Assert.False(vm.CanSubmit);   // id but no role

        vm.IsAdmin = true;
        Assert.True(vm.CanSubmit);

        Assert.Null(new AddByIdViewModel(usePermittedList: false).BuildResult()); // incomplete -> null
    }

    [Fact]
    public void Selecting_a_role_clears_any_prior_selection()
    {
        var vm = new AddByIdViewModel(usePermittedList: false) { PlayerId = "1", IsAdmin = true };
        Assert.Equal(PlayerRole.Admin, vm.SelectedRole);

        vm.IsBanned = true;
        Assert.Equal(PlayerRole.Banned, vm.SelectedRole);
        Assert.False(vm.IsAdmin);

        // A grouping-driven false (RadioButton unchecking a sibling) does not clear the selection.
        vm.IsAdmin = false;
        Assert.Equal(PlayerRole.Banned, vm.SelectedRole);
    }

    [Fact]
    public void BuildResult_carries_platform_id_trimmed_and_role()
    {
        var vm = new AddByIdViewModel(usePermittedList: true)
        {
            SelectedPlatform = PlayerPlatforms.PlayStation,
            PlayerId = "  psid  ",
            IsPermitted = true,
        };

        var result = vm.BuildResult();

        Assert.NotNull(result);
        Assert.Equal(PlayerPlatforms.PlayStation, result!.Platform);
        Assert.Equal("psid", result.PlayerId);
        Assert.Equal(PlayerRole.Permitted, result.Role);
    }

    [Fact]
    public void Platforms_lists_all_four_supported()
    {
        Assert.Equal(PlayerPlatforms.All.Count, new AddByIdViewModel(usePermittedList: false).Platforms.Count);
    }
}
