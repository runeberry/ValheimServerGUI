using Avalonia.Headless.XUnit;
using Microsoft.Extensions.DependencyInjection;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;
using Xunit;

namespace ValheimServerGUI.App.Tests.ViewModels;

public class ServerDetailsViewModelTests
{
    private static readonly System.IServiceProvider Core =
        new ServiceCollection().AddValheimCore().BuildServiceProvider();

    private static ServerDetailsViewModel Build(int port)
    {
        var vm = new ServerDetailsViewModel(Core.GetRequiredService<IIpAddressProvider>(), () => port);
        vm.SetServer(Core.GetRequiredService<ValheimServer>());
        return vm;
    }

    [AvaloniaFact]
    public void Default_state_is_loading_and_local_loopback()
    {
        var vm = Build(CoreConstants.DefaultServerPort);
        Assert.Equal("127.0.0.1", vm.LocalIp);
        Assert.Equal("N/A", vm.InviteCode);
        Assert.False(vm.InviteCodeCopyable);
    }

    [AvaloniaFact]
    public void Default_port_is_not_appended()
    {
        var vm = Build(CoreConstants.DefaultServerPort);
        vm.SetActive(true);
        Assert.Equal("127.0.0.1", vm.LocalIp);
        vm.SetActive(false);
    }

    [AvaloniaFact]
    public void Non_default_port_is_appended_on_activate()
    {
        var vm = Build(2500);
        vm.SetActive(true);
        Assert.Equal("127.0.0.1:2500", vm.LocalIp);
        vm.SetActive(false);
    }
}
