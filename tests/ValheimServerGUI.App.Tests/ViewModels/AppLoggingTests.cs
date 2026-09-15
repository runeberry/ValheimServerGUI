using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using ValheimServerGUI.App.Services;
using ValheimServerGUI.App.Tests.Fakes;
using ValheimServerGUI.App.Tests.Services;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Logging;
using Xunit;

namespace ValheimServerGUI.App.Tests.ViewModels;

// Regression: the app layer had lost nearly all of its logging in the port. These assert that ordinary
// user actions actually reach the shared application-log buffer (what the Logs tab shows).
public class AppLoggingTests
{
    private static readonly IServiceProvider Core =
        new ServiceCollection().AddValheimCore().BuildServiceProvider();

    private static MainWindowViewModel BuildVm(IApplicationLogger logger)
    {
        var shell = new ShellLauncher(new RecordingSystemShell(), TestLog.Silent);
        return new MainWindowViewModel(
            Core.GetRequiredService<IServerManager>(), new FakeUserPreferencesProvider(),
            new FakeServerPreferencesProvider(), Core.GetRequiredService<IWorldPreferencesProvider>(),
            Core.GetRequiredService<ISteamCloudWorldProvider>(), Core.GetRequiredService<IIpAddressProvider>(),
            Core.GetRequiredService<IPlayerDataRepository>(), logger,
            new FakeSoftwareUpdateProvider(), shell, Core.GetRequiredService<IValheimPathResolver>());
    }

    [Fact]
    public void Loading_a_profile_is_logged()
    {
        var logger = Core.GetRequiredService<IApplicationLogger>();
        var vm = BuildVm(logger);
        var marker = $"AppLog-{Guid.NewGuid():N}";

        vm.LoadProfile(new ServerPreferences { ProfileName = marker });

        Assert.Contains(logger.LogBuffer, line => line.Contains("Loading server profile") && line.Contains(marker));
    }
}
