using System;
using System.ComponentModel;
using System.Linq;
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

// The "Since" columns sort by the underlying timestamp, not by their relative-time text (which orders by wording:
// "just now" would land before "5 minutes ago"). Oldest first ascending, like the WinForms TimeAgo comparison.
public class SinceColumnSortTests
{
    private static readonly IServiceProvider Core =
        new ServiceCollection().AddValheimCore().BuildServiceProvider();

    private static readonly DateTimeOffset Now = DateTimeOffset.Now;
    private static readonly TimeSpan[] Ages = { TimeSpan.FromSeconds(2), TimeSpan.FromDays(2), TimeSpan.FromMinutes(5) };

    // Sorts the grid's column with the given header ascending and returns the rows in display order.
    private static T[] SortAscending<T>(Control view, string header)
    {
        var grid = view.GetVisualDescendants().OfType<DataGrid>().First();
        grid.Columns.First(c => (string?)c.Header == header).Sort(ListSortDirection.Ascending);
        Dispatcher.UIThread.RunJobs();
        return grid.CollectionView.Cast<T>().ToArray();
    }

    private static Window Show(Control view)
    {
        var window = new Window { Content = view, Width = 600, Height = 400 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    [AvaloniaFact]
    public void Players_tab_since_sorts_by_status_change_time()
    {
        var repo = new FakePlayerDataRepository();
        for (var i = 0; i < Ages.Length; i++)
            repo.PushUpdate(new PlayerInfo
            {
                Platform = "Steam", PlatformRaw = "Steam", PlayerId = i.ToString(), PlayerName = "P" + i,
                LastStatusChange = Now - Ages[i],
            });
        var shell = new ShellLauncher(new ValheimServerGUI.App.Tests.Services.RecordingSystemShell(), TestLog.Silent);
        var vm = new MainWindowViewModel(
            Core.GetRequiredService<IServerManager>(), new FakeUserPreferencesProvider(),
            new FakeServerPreferencesProvider(), Core.GetRequiredService<IWorldPreferencesProvider>(),
            Core.GetRequiredService<ISteamCloudWorldProvider>(), Core.GetRequiredService<IIpAddressProvider>(),
            repo, Core.GetRequiredService<IApplicationLogger>(), new FakeSoftwareUpdateProvider(), shell,
            Core.GetRequiredService<IValheimPathResolver>(), Core.GetRequiredService<IPlayerListImportService>(),
            Core.GetRequiredService<IRuneberryApiClient>());
        vm.LoadProfile(new ServerPreferences { ProfileName = "Sort" });
        var view = new ValheimServerGUI.App.Views.Tabs.PlayersView { DataContext = vm };
        var window = Show(view);

        var order = SortAscending<PlayerRowViewModel>(view, Strings.Players_Column_Since).Select(r => r.PlatformId);

        Assert.Equal(new[] { "1", "2", "0" }, order); // 2 days, 5 minutes, just now
        window.Close();
    }

    [AvaloniaFact]
    public void Known_characters_since_sorts_by_last_seen()
    {
        var player = new PlayerInfo
        {
            Platform = "Steam", PlayerId = "1",
            Characters = Ages
                .Select((age, i) => new PlayerInfo.CharacterInfo { CharacterName = "C" + i, MatchConfident = true, LastSeen = Now - age })
                .ToList(),
        };
        var vm = new KnownCharactersViewModel();
        vm.Load(player);
        var view = new KnownCharactersView { DataContext = vm };
        var window = Show(view);

        var order = SortAscending<CharacterRowViewModel>(view, Strings.KnownCharacters_Column_Since).Select(r => r.CharacterName);

        Assert.Equal(new[] { "C1", "C2", "C0" }, order);
        window.Close();
    }
}
