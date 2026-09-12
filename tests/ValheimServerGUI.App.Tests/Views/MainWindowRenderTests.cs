using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using ValheimServerGUI.App.Services;
using ValheimServerGUI.App.Tests.Fakes;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.App.Views;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Logging;
using Xunit;

namespace ValheimServerGUI.App.Tests.Views;

// Tier-3: the real window renders (all tabs/menus bind cleanly) and button enablement tracks status.
public class MainWindowRenderTests
{
    private static readonly System.IServiceProvider Core =
        new ServiceCollection().AddValheimCore().BuildServiceProvider();

    private static MainWindowViewModel BuildViewModel()
    {
        var shell = new ShellLauncher(new Services.RecordingSystemShell(), TestLog.Silent);
        var vm = new MainWindowViewModel(
            Core.GetRequiredService<ValheimServer>(),
            new FakeUserPreferencesProvider(),
            new FakeServerPreferencesProvider(),
            Core.GetRequiredService<IWorldPreferencesProvider>(),
            Core.GetRequiredService<ISteamCloudWorldProvider>(),
            Core.GetRequiredService<IIpAddressProvider>(),
            Core.GetRequiredService<IPlayerDataRepository>(),
            Core.GetRequiredService<IApplicationLogger>(),
            new FakeSoftwareUpdateProvider(),
            shell,
            Core.GetRequiredService<IValheimPathResolver>());
        vm.LoadProfile(new ServerPreferences { ProfileName = "Render" });
        return vm;
    }

    private static Button FindButton(Visual root, string content)
        => root.GetVisualDescendants().OfType<Button>().First(b => (b.Content as string) == content);

    [AvaloniaFact]
    public void Window_renders_all_tabs_without_binding_errors()
    {
        var window = new MainWindow(BuildViewModel());
        window.Show();

        Assert.True(window.IsVisible);
        // Rendering the frame forces the whole visual tree (menus + 5 tabs) to realize + bind.
        Assert.NotNull(window.CaptureRenderedFrame());
    }

    [AvaloniaFact]
    public void Start_stop_buttons_track_server_status()
    {
        var vm = BuildViewModel();
        var window = new MainWindow(vm);
        window.Show();

        var start = FindButton(window, "Start");
        var stop = FindButton(window, "Stop");

        vm.ServerStatus = ServerStatus.Stopped;
        Assert.True(start.IsEffectivelyEnabled);
        Assert.False(stop.IsEffectivelyEnabled);

        vm.ServerStatus = ServerStatus.Running;
        Assert.False(start.IsEffectivelyEnabled);
        Assert.True(stop.IsEffectivelyEnabled);
    }
}
