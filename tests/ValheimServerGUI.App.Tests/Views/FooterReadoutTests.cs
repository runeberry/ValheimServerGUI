using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using ValheimServerGUI.App.Controls;
using ValheimServerGUI.App.Services;
using ValheimServerGUI.App.Tests.Services;
using ValheimServerGUI.App.Tests.Fakes;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Logging;
using Xunit;

namespace ValheimServerGUI.App.Tests.Views;

// Regression: the footer update readout used to be a Button that went disabled when the status wasn't a
// clickable link (e.g. "Up to date"), dimming the text to a near-invisible ~40%-alpha colour and the icon
// with it. The readout must render its text at full contrast when it is not a link.
public class FooterReadoutTests
{
    private static readonly System.IServiceProvider Core =
        new ServiceCollection().AddValheimCore().BuildServiceProvider();

    [AvaloniaFact]
    public void Up_to_date_readout_text_is_opaque()
    {
        var update = new FakeSoftwareUpdateProvider();
        var shell = new ShellLauncher(new RecordingSystemShell(), TestLog.Silent);
        var vm = new MainWindowViewModel(
            Core.GetRequiredService<IServerManager>(), new FakeUserPreferencesProvider(),
            new FakeServerPreferencesProvider(), Core.GetRequiredService<IWorldPreferencesProvider>(),
            Core.GetRequiredService<ISteamCloudWorldProvider>(), Core.GetRequiredService<IIpAddressProvider>(),
            Core.GetRequiredService<IPlayerDataRepository>(), Core.GetRequiredService<IApplicationLogger>(),
            update, shell, Core.GetRequiredService<IValheimPathResolver>());

        var window = new ValheimServerGUI.App.Views.MainWindow(vm);
        window.Show();
        var current = ValheimServerGUI.Tools.AssemblyHelper.GetApplicationVersion();
        update.RaiseFinished(new SoftwareUpdateEventArgs(current, isManualCheck: false));
        Dispatcher.UIThread.RunJobs();
        window.Measure(new Size(700, 500));
        window.Arrange(new Rect(new Size(700, 500)));

        Assert.False(vm.UpdateIsLink); // "up to date" is not a link
        var readout = window.GetVisualDescendants().OfType<TextBlock>()
            .First(t => t.Text != null && t.Text.StartsWith("Up to date") && t.IsVisible);
        var color = (readout.Foreground as ISolidColorBrush)!.Color;
        Assert.Equal(255, color.A); // fully opaque, not the dimmed disabled colour
    }

    [AvaloniaFact]
    public void Update_available_readout_is_a_visible_hyperlink_label()
    {
        var update = new FakeSoftwareUpdateProvider();
        var shell = new ShellLauncher(new RecordingSystemShell(), TestLog.Silent);
        var vm = new MainWindowViewModel(
            Core.GetRequiredService<IServerManager>(), new FakeUserPreferencesProvider(),
            new FakeServerPreferencesProvider(), Core.GetRequiredService<IWorldPreferencesProvider>(),
            Core.GetRequiredService<ISteamCloudWorldProvider>(), Core.GetRequiredService<IIpAddressProvider>(),
            Core.GetRequiredService<IPlayerDataRepository>(), Core.GetRequiredService<IApplicationLogger>(),
            update, shell, Core.GetRequiredService<IValheimPathResolver>());

        var window = new ValheimServerGUI.App.Views.MainWindow(vm);
        window.Show();
        update.RaiseFinished(new SoftwareUpdateEventArgs("999.0.0", isManualCheck: false));
        Dispatcher.UIThread.RunJobs();
        window.Measure(new Size(700, 500));
        window.Arrange(new Rect(new Size(700, 500)));

        Assert.True(vm.UpdateIsLink); // a newer version is a link
        var link = window.GetVisualDescendants().OfType<HyperlinkLabel>()
            .First(l => l.IsVisible);
        Assert.StartsWith("Update available", link.Text!);
    }
}
