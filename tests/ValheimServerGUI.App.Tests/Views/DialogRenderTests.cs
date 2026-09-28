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
        => Realize(new AddPlayerWindow(ValheimServerGUI.App.ViewModels.Dialogs.AddPlayerOptions.ForMyAccounts));

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
        var sections = new[] { vm.MyAccounts, vm.Friends, vm.Banned }
            .Select(s => new PlayerListSectionView { DataContext = s })
            .ToList();

        string[] Visible(PlayerListSectionView v) => v.GetLogicalDescendants().OfType<ValheimServerGUI.App.Controls.DataListView>()
            .First(l => l.Name == "AccountsList").Columns.Where(c => c.IsVisible).Select(c => (string)c.Header!).ToArray();

        Assert.Equal(new[] { "Player Name", "Platform ID", "Default Role" }, Visible(sections[0]));
        Assert.Equal(new[] { "Player Name", "Platform ID", "Default Role" }, Visible(sections[1]));
        Assert.Equal(new[] { "Player Name", "Platform ID" }, Visible(sections[2]));
    }

    [AvaloniaFact]
    public void BugReportWindow_realizes()
        => Realize(new BugReportWindow(new BugReportViewModel(new FakeRuneberryApiClient())));
}
