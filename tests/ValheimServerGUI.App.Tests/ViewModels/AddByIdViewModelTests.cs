using ValheimServerGUI.App.ViewModels.Dialogs;
using ValheimServerGUI.Tools.Models;
using Xunit;

namespace ValheimServerGUI.App.Tests.ViewModels;

public class AddByIdViewModelTests
{
    [Fact]
    public void CanSubmit_requires_an_id_and_at_least_one_list()
    {
        var vm = new AddByIdViewModel();
        Assert.False(vm.CanSubmit);

        vm.PlayerId = "123";
        Assert.False(vm.CanSubmit);           // id but no list

        vm.AddBanned = true;
        Assert.True(vm.CanSubmit);

        Assert.Null(new AddByIdViewModel().BuildResult()); // incomplete form -> null
    }

    [Fact]
    public void ShowBanWarning_only_when_ban_combined_with_admin_or_permit()
    {
        var vm = new AddByIdViewModel { PlayerId = "1", AddBanned = true };
        Assert.False(vm.ShowBanWarning);

        vm.AddAdmin = true;
        Assert.True(vm.ShowBanWarning);
    }

    [Fact]
    public void BuildResult_carries_platform_id_and_list_choices_trimmed()
    {
        var vm = new AddByIdViewModel
        {
            SelectedPlatform = PlayerPlatforms.PlayStation,
            PlayerId = "  psid  ",
            AddPermitted = true,
        };

        var result = vm.BuildResult();

        Assert.NotNull(result);
        Assert.Equal(PlayerPlatforms.PlayStation, result!.Platform);
        Assert.Equal("psid", result.PlayerId);
        Assert.True(result.Permitted);
        Assert.False(result.Admin);
    }

    [Fact]
    public void Platforms_lists_all_four_supported()
    {
        Assert.Equal(PlayerPlatforms.All.Count, new AddByIdViewModel().Platforms.Count);
    }
}
