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
using ValheimServerGUI.Localization;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Models;
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
    public void AddPlayerWindow_realizes()
        => Realize(new AddPlayerWindow(ValheimServerGUI.App.ViewModels.Dialogs.AddPlayerOptions.ForServer(usePermittedList: true)));

    [AvaloniaFact]
    public void ManagePlayersWindow_realizes()
    {
        var repo = Core.GetRequiredService<IPlayerDataRepository>();
        Realize(new ManagePlayersWindow(
            new ValheimServerGUI.App.ViewModels.Dialogs.ManagePlayersViewModel(new FakeUserPreferencesProvider(), repo, null)));
    }

    // "View Player Details" opens Manage Players focused on one player: the window shows that player's tab, and the
    // row is scrolled into view even far down a long list (the inner DataGrid only scrolls for clicks/keys).
    [AvaloniaFact]
    public void ManagePlayers_focus_shows_the_players_tab_and_scrolls_the_row_into_view()
    {
        var repo = new FakePlayerDataRepository();
        var prefs = new FakeUserPreferencesProvider();
        var defaults = new UserPreferences();
        for (var i = 0; i < 60; i++)
        {
            var id = i.ToString("D2");
            repo.PushUpdate(new PlayerInfo { Platform = "Steam", PlatformRaw = "Steam", PlayerId = id, PlayerName = "Banned" + id });
            defaults.PlayerDefaults["Steam:" + id] = new PlayerDefaultEntry(PlayerRole.Banned, "Steam");
        }
        prefs.SavePreferences(defaults);
        var vm = new ValheimServerGUI.App.ViewModels.Dialogs.ManagePlayersViewModel(prefs, repo, null);
        vm.FocusPlayer("Steam:59");

        var window = new ManagePlayersWindow(vm);
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1, window.GetVisualDescendants().OfType<TabControl>().Single().SelectedIndex);
        var realized = window.GetVisualDescendants().OfType<DataGridRow>()
            .Select(r => r.DataContext).OfType<ValheimServerGUI.App.ViewModels.PlayerRowViewModel>();
        Assert.Contains(realized, r => r.Key == "Steam:59");
        window.Close();
    }

    // Both tabs carry the Known Characters table, and the account row menu offers edit-name and copy-ID.
    [AvaloniaFact]
    public void ManagePlayers_tabs_have_known_characters_and_the_name_and_id_actions()
    {
        var repo = Core.GetRequiredService<IPlayerDataRepository>();
        var vm = new ValheimServerGUI.App.ViewModels.Dialogs.ManagePlayersViewModel(new FakeUserPreferencesProvider(), repo, null);

        foreach (var section in new[] { vm.PlayerAccounts, vm.Banned })
        {
            var view = new PlayerListSectionView { DataContext = section };
            var window = new Window { Content = view };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            var characters = view.GetVisualDescendants().OfType<ValheimServerGUI.App.Views.KnownCharactersView>().Single();
            Assert.True(characters.IsEffectivelyVisible);
            var menu = view.GetLogicalDescendants().OfType<ValheimServerGUI.App.Controls.DataListView>()
                .First(l => l.Name == "AccountsList").RowContextMenu!;
            var headers = menu.Items.OfType<MenuItem>().Select(m => m.Header as string).ToList();
            Assert.Contains(Strings.ManagePlayers_Menu_EditName, headers);
            Assert.Contains(Strings.ManagePlayers_Menu_CopyId, headers);
            window.Close();
        }
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

        Assert.Equal(new[] { Strings.ManagePlayers_Column_PlayerName, Strings.ManagePlayers_Column_PlatformId, Strings.ManagePlayers_Column_DefaultRole }, Visible(sections[0]));
        Assert.Equal(new[] { Strings.ManagePlayers_Column_PlayerName, Strings.ManagePlayers_Column_PlatformId }, Visible(sections[1]));
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

        var setRole = menu.Items.OfType<MenuItem>().First(m => (string?)m.Header == Strings.ManagePlayers_Menu_SetDefaultRole);
        var none = setRole.Items.OfType<MenuItem>().First(m => (string?)m.Header == Strings.Role_None);
        var banned = setRole.Items.OfType<MenuItem>().First(m => (string?)m.Header == Strings.Role_Banned);
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
