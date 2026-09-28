using Avalonia.VisualTree;
using Avalonia.LogicalTree;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using ValheimServerGUI.App.Tests.Fakes;
using ValheimServerGUI.App.ViewModels.Dialogs;
using ValheimServerGUI.App.Views.Dialogs;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;
using Xunit;

namespace ValheimServerGUI.App.Tests.Views;

// The dialogs migrated to the FormField controls realize + bind without throwing. Dialogs aren't opened
// by the boot smoke, so this is the guard that their FormField wiring (and DataContext inheritance into
// the wrappers) is sound.
public class DialogRenderTests
{
    private static readonly System.IServiceProvider Core =
        new ServiceCollection().AddValheimCore().BuildServiceProvider();

    private static void Realize(Window window)
    {
        window.Show();
        Assert.True(window.IsVisible);
        Dispatcher.UIThread.RunJobs();
        window.Measure(new Size(640, 640));
        window.Arrange(new Rect(new Size(640, 640)));
        window.Close();
    }

    [AvaloniaFact]
    public void PreferencesWindow_realizes()
        => Realize(new PreferencesWindow(new PreferencesViewModel(new FakeUserPreferencesProvider(), new FakeStartupManager())));

    [AvaloniaFact]
    public void DirectoriesWindow_realizes()
        => Realize(new DirectoriesWindow(new DirectoriesViewModel(
            new FakeUserPreferencesProvider(), Core.GetRequiredService<IValheimPathResolver>(), new RecordingShellLauncher())));

    [AvaloniaFact]
    public void WorldPreferencesWindow_realizes()
        => Realize(new WorldPreferencesWindow(new WorldPreferencesViewModel(
            Core.GetRequiredService<IWorldPreferencesProvider>(), "TestWorld", new RecordingShellLauncher())));

    [AvaloniaFact]
    public void PlayerDetailsWindow_realizes()
        => Realize(new PlayerDetailsWindow(new PlayerDetailsViewModel(
            Core.GetRequiredService<IPlayerDataRepository>(), "steam-1")));

    [AvaloniaFact]
    public void AddPlayerWindow_realizes()
        => Realize(new AddPlayerWindow(ValheimServerGUI.App.ViewModels.Dialogs.AddPlayerOptions.ForServer(usePermittedList: true)));

    [AvaloniaFact]
    public void ManagePlayersWindow_realizes()
    {
        var repo = Core.GetRequiredService<IPlayerDataRepository>();
        Realize(new ManagePlayersWindow(
            new ValheimServerGUI.App.ViewModels.Dialogs.ManagePlayersViewModel(new FakeUserPreferencesProvider(), repo, null),
            repo, null));
    }

    // Banned entries are always Banned, so that tab drops the Default Role column; the other tabs keep it last.
    [AvaloniaFact]
    public void ManagePlayers_banned_tab_has_no_role_column()
    {
        var repo = Core.GetRequiredService<IPlayerDataRepository>();
        var vm = new ValheimServerGUI.App.ViewModels.Dialogs.ManagePlayersViewModel(new FakeUserPreferencesProvider(), repo, null);
        var sections = new[] { vm.PlayerAccounts, vm.Banned }
            .Select(s => new PlayerListSectionView { DataContext = s })
            .ToList();

        string[] Visible(PlayerListSectionView v) => v.GetLogicalDescendants().OfType<ValheimServerGUI.App.Controls.DataListView>()
            .First(l => l.Name == "AccountsList").Columns.Where(c => c.IsVisible).Select(c => (string)c.Header!).ToArray();

        Assert.Equal(new[] { "Player Name", "Platform ID", "Default Role" }, Visible(sections[0]));
        Assert.Equal(new[] { "Player Name", "Platform ID" }, Visible(sections[1]));
    }

    // The "Set default role" submenu's radio items write through their TwoWay IsChecked binding: checking Banned
    // on an open context menu moves the row to the Banned tab.
    [AvaloniaFact]
    public void ManagePlayers_set_default_role_submenu_moves_the_player()
    {
        var repo = new FakePlayerDataRepository();
        repo.PushUpdate(new PlayerInfo { Platform = "Steam", PlatformRaw = "Steam", PlayerId = "1", PlayerName = "Loki" });
        var vm = new ValheimServerGUI.App.ViewModels.Dialogs.ManagePlayersViewModel(new FakeUserPreferencesProvider(), repo, null);
        var view = new PlayerListSectionView { DataContext = vm.PlayerAccounts };
        var window = new Window { Content = view, Width = 560, Height = 420 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        vm.PlayerAccounts.SelectedAccount = vm.PlayerAccounts.Accounts[0];

        var list = view.GetLogicalDescendants().OfType<ValheimServerGUI.App.Controls.DataListView>()
            .First(l => l.Name == "AccountsList");
        var menu = list.RowContextMenu!;
        menu.Open(list.GetVisualDescendants().OfType<DataGrid>().First()); // attached to the inner grid
        Dispatcher.UIThread.RunJobs();

        var setRole = menu.Items.OfType<MenuItem>().First(m => (string?)m.Header == "Set default role");
        var none = setRole.Items.OfType<MenuItem>().First(m => (string?)m.Header == "None");
        var banned = setRole.Items.OfType<MenuItem>().First(m => (string?)m.Header == "Banned");
        Assert.True(none.IsChecked); // reflects the current (absent) default

        banned.IsChecked = true;     // what a click on a radio item does
        Dispatcher.UIThread.RunJobs();
        menu.Close();

        Assert.Empty(vm.PlayerAccounts.Accounts);
        Assert.Equal("Steam:1", Assert.Single(vm.Banned.Accounts).Key);
    }

    [AvaloniaFact]
    public void BugReportWindow_realizes()
        => Realize(new BugReportWindow(new BugReportViewModel(new FakeRuneberryApiClient())));
}
