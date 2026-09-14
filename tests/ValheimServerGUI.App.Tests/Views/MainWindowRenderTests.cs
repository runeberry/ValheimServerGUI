using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
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
            Core.GetRequiredService<IServerManager>(),
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

    private static Button FindNamedButton(Visual root, string name)
        => root.GetVisualDescendants().OfType<Button>().First(b => b.Name == name);

    // Realizes + binds every tab's view (menus + all 5 tabs) by selecting each tab and forcing a
    // layout pass, which attaches and data-binds the selected tab's content. We deliberately do NOT
    // call CaptureRenderedFrame(): that forces a real Skia compositor commit which, under CPU load,
    // reads a thread-affine Brush off the wrong thread (Dispatcher.VerifyAccess throws) and dead-locks
    // the synchronously-waiting headless UI thread. Layout gives the same "everything realizes + binds
    // without throwing" coverage without the racy render. See the apptests-headless-deadlock note.
    [AvaloniaFact]
    public void Window_realizes_all_tabs_without_errors()
    {
        var window = new MainWindow(BuildViewModel());
        window.Show();
        Assert.True(window.IsVisible);

        var tabs = window.GetVisualDescendants().OfType<TabControl>().Single();
        Assert.Equal(5, tabs.ItemCount);

        for (var i = 0; i < tabs.ItemCount; i++)
        {
            tabs.SelectedIndex = i;
            ForceLayout(window);

            var item = Assert.IsType<TabItem>(tabs.Items[i]);
            var view = Assert.IsAssignableFrom<Control>(item.Content);
            Assert.True(view.IsAttachedToVisualTree(), $"Tab {i} ('{item.Header}') content did not realize.");
        }
    }

    // Flush the selection-changed handler, then run a synchronous measure/arrange so the newly-selected
    // tab's content template instantiates and binds. Layout does not touch the compositor.
    private static void ForceLayout(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.Measure(RenderSize);
        window.Arrange(new Rect(RenderSize));
    }

    private static readonly Size RenderSize = new(1280, 800);

    [AvaloniaFact]
    public void Start_stop_buttons_track_server_status()
    {
        var vm = BuildViewModel();
        var window = new MainWindow(vm);
        window.Show();

        // The Start/Stop buttons live on the (default-selected) Server Controls tab; realize it.
        ForceLayout(window);

        var start = FindNamedButton(window, "StartButton");
        var stop = FindNamedButton(window, "StopButton");

        vm.ServerStatus = ServerStatus.Stopped;
        Assert.True(start.IsEffectivelyEnabled);
        Assert.False(stop.IsEffectivelyEnabled);

        vm.ServerStatus = ServerStatus.Running;
        Assert.False(start.IsEffectivelyEnabled);
        Assert.True(stop.IsEffectivelyEnabled);
    }
}
