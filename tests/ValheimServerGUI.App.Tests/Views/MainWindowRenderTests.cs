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
using ValheimServerGUI.Localization;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Logging;
using Xunit;

namespace ValheimServerGUI.App.Tests.Views;

// Tier-3: the real window renders (all tabs/menus bind cleanly) and button enablement tracks status.
public class MainWindowRenderTests
{
    private static readonly System.IServiceProvider Core =
        new ServiceCollection().AddValheimCore().BuildServiceProvider();

    private static MainWindowViewModel BuildViewModel(params string[] profiles)
    {
        var shell = new ShellLauncher(new Services.RecordingSystemShell(), TestLog.Silent);
        var vm = new MainWindowViewModel(
            Core.GetRequiredService<IServerManager>(),
            new FakeUserPreferencesProvider(),
            new FakeServerPreferencesProvider(profiles.Select(p => new ServerPreferences { ProfileName = p })),
            Core.GetRequiredService<IWorldPreferencesProvider>(),
            Core.GetRequiredService<ISteamCloudWorldProvider>(),
            Core.GetRequiredService<IIpAddressProvider>(),
            Core.GetRequiredService<IPlayerDataRepository>(),
            Core.GetRequiredService<IApplicationLogger>(),
            new FakeSoftwareUpdateProvider(),
            shell,
            Core.GetRequiredService<IValheimPathResolver>(),
            Core.GetRequiredService<IPlayerListImportService>(),
            Core.GetRequiredService<IRuneberryApiClient>());
        vm.LoadProfile(new ServerPreferences { ProfileName = profiles.FirstOrDefault() ?? "Render" });
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

    // Regression: the File menu opens and its dynamic Load/Remove Profile submenus realize their generated
    // items without crashing. The items must (a) bind their command across the submenu popup (Tree=Logical)
    // so they are enabled, and (b) survive CanExecute being evaluated with a non-string parameter (the
    // transient inherited DataContext during container setup) rather than throwing and killing the menu.
    [AvaloniaFact]
    public void File_menu_and_dynamic_profile_submenus_open_without_crashing()
    {
        var vm = BuildViewModel("Alpha", "Bravo");
        var window = new MainWindow(vm);
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var menu = window.GetVisualDescendants().OfType<Menu>().Single();

        foreach (var top in new[] { "File", "Help" })
        {
            var item = menu.Items.OfType<MenuItem>().First(m => (m.Header as string)?.Contains(top) == true);
            Assert.Null(Record.Exception(() => item.Open()));
            Dispatcher.UIThread.RunJobs();
        }

        var file = menu.Items.OfType<MenuItem>().First(m => (m.Header as string)?.Contains("File") == true);
        file.Open();
        Dispatcher.UIThread.RunJobs();

        foreach (var dynamicName in new[] { "Load", "Remove" })
        {
            var dyn = file.Items.OfType<MenuItem>().First(m => (m.Header as string)?.Contains(dynamicName) == true);
            Assert.Null(Record.Exception(() => dyn.Open()));
            Dispatcher.UIThread.RunJobs();
            ForceLayout(window);

            var children = dyn.Items.OfType<object>()
                .Select(i => (dyn.ContainerFromItem(i) as MenuItem) ?? i as MenuItem)
                .Where(mi => mi is not null)
                .ToList();

            Assert.Equal(2, children.Count); // Alpha + Bravo
            foreach (var child in children)
            {
                Assert.NotNull(child!.Command);                    // command resolved across the popup
                Assert.IsType<string>(child.CommandParameter);     // parameter is the profile name, not the VM
                Assert.True(child.IsEffectivelyEnabled);           // ungated → enabled (bug 2)
                Assert.Null(Record.Exception(() => child.Command!.CanExecute(vm))); // no throw on a VM param
            }
        }
    }

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

    // The Players tab's "Set server role" submenu: the fourth item names the default it falls back to, and
    // checking a radio item writes this server's override through the TwoWay binding.
    [AvaloniaFact]
    public void Players_tab_server_role_submenu_writes_the_override()
    {
        var repo = new Fakes.FakePlayerDataRepository();
        var player = new PlayerInfo { Platform = "Steam", PlatformRaw = "Steam", PlayerId = "1", PlayerName = "Odin" };
        repo.PushUpdate(player);
        var prefs = new UserPreferences();
        prefs.PlayerDefaults[player.Key] = new PlayerDefaultEntry(PlayerRole.Permitted, "Steam");
        var shell = new ShellLauncher(new Services.RecordingSystemShell(), TestLog.Silent);
        var vm = new MainWindowViewModel(
            Core.GetRequiredService<IServerManager>(), new FakeUserPreferencesProvider(prefs),
            new FakeServerPreferencesProvider(), Core.GetRequiredService<IWorldPreferencesProvider>(),
            Core.GetRequiredService<ISteamCloudWorldProvider>(), Core.GetRequiredService<IIpAddressProvider>(),
            repo, Core.GetRequiredService<IApplicationLogger>(), new FakeSoftwareUpdateProvider(), shell,
            Core.GetRequiredService<IValheimPathResolver>(), Core.GetRequiredService<IPlayerListImportService>(),
            Core.GetRequiredService<IRuneberryApiClient>());
        vm.LoadProfile(new ServerPreferences { ProfileName = "Menu" });
        var view = new ValheimServerGUI.App.Views.Tabs.PlayersView { DataContext = vm };
        var window = new Window { Content = view, Width = 550, Height = 400 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        vm.Players.SelectedPlayer = vm.Players.Players[0];

        var list = view.GetVisualDescendants().OfType<ValheimServerGUI.App.Controls.DataListView>().First();
        var menu = list.RowContextMenu!;
        menu.Open(list.GetVisualDescendants().OfType<DataGrid>().First()); // attached to the inner grid
        Dispatcher.UIThread.RunJobs();
        var setRole = menu.Items.OfType<MenuItem>().First(m => (string?)m.Header == Strings.Players_Menu_SetServerRole);
        var items = setRole.Items.OfType<MenuItem>().ToList();

        Assert.Equal(new[] { Strings.Role_Admin, Strings.Role_Permitted, Strings.Role_Banned, "Default role (Permitted)" }, items.Select(m => (string?)m.Header));
        Assert.True(items[3].IsChecked); // no override yet

        items[0].IsChecked = true;       // what a click on a radio item does
        Dispatcher.UIThread.RunJobs();
        menu.Close();

        Assert.Equal(PlayerRole.Admin, vm.Form.GetOverride(player.Key));
    }
}
