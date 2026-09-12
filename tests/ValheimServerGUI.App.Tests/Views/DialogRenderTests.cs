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
            new FakeUserPreferencesProvider(), Core.GetRequiredService<IValheimPathResolver>())));

    [AvaloniaFact]
    public void WorldPreferencesWindow_realizes()
        => Realize(new WorldPreferencesWindow(new WorldPreferencesViewModel(
            Core.GetRequiredService<IWorldPreferencesProvider>(), "TestWorld")));

    [AvaloniaFact]
    public void PlayerDetailsWindow_realizes()
        => Realize(new PlayerDetailsWindow(new PlayerDetailsViewModel(
            Core.GetRequiredService<IPlayerDataRepository>(), "steam-1")));

    [AvaloniaFact]
    public void BugReportWindow_realizes()
        => Realize(new BugReportWindow(new BugReportViewModel(new FakeRuneberryApiClient())));
}
