using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using ValheimServerGUI.App.Services;
using ValheimServerGUI.App.Tests.Services;
using ValheimServerGUI.App.Tests.Fakes;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Logging;
using Xunit;

namespace ValheimServerGUI.App.Tests.Views;

// The tab titles used to render in Fluent's SemiLight/13 font, visibly different from the app body. The
// custom tab theme pins them to the app font (Normal weight, 12px) for consistency.
public class TabFontConsistencyTests
{
    private static readonly System.IServiceProvider Core =
        new ServiceCollection().AddValheimCore().BuildServiceProvider();

    [AvaloniaFact]
    public void Tab_titles_use_the_app_body_font()
    {
        var shell = new ShellLauncher(new RecordingSystemShell(), TestLog.Silent);
        var vm = new MainWindowViewModel(
            Core.GetRequiredService<ValheimServer>(), new FakeUserPreferencesProvider(),
            new FakeServerPreferencesProvider(), Core.GetRequiredService<IWorldPreferencesProvider>(),
            Core.GetRequiredService<ISteamCloudWorldProvider>(), Core.GetRequiredService<IIpAddressProvider>(),
            Core.GetRequiredService<IPlayerDataRepository>(), Core.GetRequiredService<IApplicationLogger>(),
            new FakeSoftwareUpdateProvider(), shell, Core.GetRequiredService<IValheimPathResolver>());
        vm.LoadProfile(new ServerPreferences { ProfileName = "Font" });

        var window = new ValheimServerGUI.App.Views.MainWindow(vm);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.Measure(new Size(700, 500));
        window.Arrange(new Rect(new Size(700, 500)));

        foreach (var tab in window.GetVisualDescendants().OfType<TabItem>())
        {
            Assert.Equal(12, tab.FontSize);
            Assert.Equal(FontWeight.Normal, tab.FontWeight);
        }
    }
}
