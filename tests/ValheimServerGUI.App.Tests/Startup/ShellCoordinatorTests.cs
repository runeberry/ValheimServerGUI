using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Microsoft.Extensions.DependencyInjection;
using ValheimServerGUI.App.Infrastructure;
using ValheimServerGUI.App.Startup;
using ValheimServerGUI.App.Tests.Fakes;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;
using Xunit;

namespace ValheimServerGUI.App.Tests.Startup;

// §2.3 orchestration: selection → window creation → show → profile load → minimized.
public class ShellCoordinatorTests
{
    // A real transient ValheimServer over the Core singletons (XDG is redirected suite-wide), so each
    // window gets a genuine per-window server — exactly the production factory relationship.
    private static readonly IServiceProvider Core =
        new ServiceCollection().AddValheimCore().BuildServiceProvider();

    private static ServerPreferences Profile(string name, bool autoStart = false, int savedDaysAgo = 0)
        => new() { ProfileName = name, AutoStart = autoStart, LastSaved = DateTime.UtcNow.AddDays(-savedDaysAgo) };

    private static (ShellCoordinator Coordinator, FakeServerPreferencesProvider ServerPrefs, FakeUserPreferencesProvider UserPrefs)
        Build(IEnumerable<ServerPreferences>? profiles = null, UserPreferences? userPrefs = null, string? argProfile = null)
    {
        var serverPrefs = new FakeServerPreferencesProvider(profiles);
        var userPrefsProvider = new FakeUserPreferencesProvider(userPrefs);
        var shell = new ValheimServerGUI.App.Services.ShellLauncher(new Services.RecordingSystemShell(), TestLog.Silent);
        var pathResolver = Core.GetRequiredService<IValheimPathResolver>();
        Func<MainWindowViewModel> factory = () =>
            new MainWindowViewModel(
                Core.GetRequiredService<ValheimServer>(), userPrefsProvider, serverPrefs,
                Core.GetRequiredService<IWorldPreferencesProvider>(),
                Core.GetRequiredService<ISteamCloudWorldProvider>(),
                Core.GetRequiredService<IIpAddressProvider>(),
                Core.GetRequiredService<IPlayerDataRepository>(),
                Core.GetRequiredService<ValheimServerGUI.Tools.Logging.IApplicationLogger>(),
                new FakeSoftwareUpdateProvider(), shell, pathResolver);
        var startupService = new StartupService(new FakeSoftwareUpdateProvider(), new FakePlayerDataRepository(), TestLog.Silent);

        var coordinator = new ShellCoordinator(
            serverPrefs, userPrefsProvider, new StartupArgsProvider(argProfile is null ? Array.Empty<string>() : new[] { argProfile }),
            factory, new WindowManager(), startupService);

        return (coordinator, serverPrefs, userPrefsProvider);
    }

    [AvaloniaFact]
    public void No_profiles_creates_and_shows_a_default_window()
    {
        var (coordinator, serverPrefs, _) = Build();

        var window = Assert.Single(coordinator.CreateAndShowStartupWindows());

        Assert.True(window.IsVisible);
        Assert.Equal(CoreConstants.DefaultServerProfileName, window.ViewModel!.CurrentProfile!.ProfileName);
        // The new Default profile was persisted.
        Assert.NotNull(serverPrefs.LoadPreferences(CoreConstants.DefaultServerProfileName));
    }

    [AvaloniaFact]
    public void Single_profile_opens_one_window_loaded_with_it()
    {
        var (coordinator, _, userPrefs) = Build(new[] { Profile("Solo", savedDaysAgo: 1) });

        var window = Assert.Single(coordinator.CreateAndShowStartupWindows());

        Assert.Equal("Solo", window.ViewModel!.CurrentProfile!.ProfileName);
        Assert.False(window.ViewModel.AutoStartOnLoad);
        Assert.Equal("Solo", userPrefs.LoadPreferences().LastActiveProfile); // last-active recorded (§16.2)
    }

    [AvaloniaFact]
    public void Autostart_profiles_each_open_a_window_flagged_to_start()
    {
        var (coordinator, _, _) = Build(new[]
        {
            Profile("Alpha", autoStart: true),
            Profile("Beta"),
            Profile("Gamma", autoStart: true),
        });

        var windows = coordinator.CreateAndShowStartupWindows();

        Assert.Equal(2, windows.Count);
        Assert.All(windows, w => Assert.True(w.ViewModel!.AutoStartOnLoad));
        Assert.Equal(new[] { "Alpha", "Gamma" },
            windows.Select(w => w.ViewModel!.CurrentProfile!.ProfileName));
    }

    [AvaloniaFact]
    public void StartMinimized_opens_the_window_minimized()
    {
        var (coordinator, _, _) = Build(
            new[] { Profile("Solo") },
            userPrefs: new UserPreferences { StartMinimized = true });

        var window = Assert.Single(coordinator.CreateAndShowStartupWindows());

        Assert.Equal(WindowState.Minimized, window.WindowState);
    }

    [AvaloniaFact]
    public void Command_line_profile_opens_just_that_profile()
    {
        var (coordinator, _, _) = Build(
            new[] { Profile("Alpha", savedDaysAgo: 1), Profile("Beta", savedDaysAgo: 5) },
            argProfile: "Beta");

        var window = Assert.Single(coordinator.CreateAndShowStartupWindows());
        Assert.Equal("Beta", window.ViewModel!.CurrentProfile!.ProfileName);
    }
}
