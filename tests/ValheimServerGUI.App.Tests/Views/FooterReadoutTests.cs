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
using ValheimServerGUI.Localization;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Logging;
using Xunit;

namespace ValheimServerGUI.App.Tests.Views;

// When the update status is not a clickable link (e.g. "Up to date"), the footer readout must still render its
// text and icon at full contrast, not dimmed to the ~40% alpha of a disabled control.
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
            update, shell, Core.GetRequiredService<IValheimPathResolver>(),
            Core.GetRequiredService<IPlayerListImportService>());

        var window = new ValheimServerGUI.App.Views.MainWindow(vm);
        window.Show();
        var current = ValheimServerGUI.Tools.AssemblyHelper.GetApplicationVersion();
        update.RaiseFinished(new SoftwareUpdateEventArgs(current, "https://forge.example/r", isManualCheck: false));
        Dispatcher.UIThread.RunJobs();
        window.Measure(new Size(700, 500));
        window.Arrange(new Rect(new Size(700, 500)));

        Assert.False(vm.UpdateIsLink); // "up to date" is not a link
        var readout = window.GetVisualDescendants().OfType<TextBlock>()
            .First(t => t.Text != null && t.Text == string.Format(Strings.Update_StatusUpToDate, current) && t.IsVisible);
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
            update, shell, Core.GetRequiredService<IValheimPathResolver>(),
            Core.GetRequiredService<IPlayerListImportService>());

        var window = new ValheimServerGUI.App.Views.MainWindow(vm);
        window.Show();
        update.RaiseFinished(new SoftwareUpdateEventArgs("999.0.0", "https://forge.example/r", isManualCheck: false));
        Dispatcher.UIThread.RunJobs();
        window.Measure(new Size(700, 500));
        window.Arrange(new Rect(new Size(700, 500)));

        Assert.True(vm.UpdateIsLink); // a newer version is a link
        var link = window.GetVisualDescendants().OfType<HyperlinkLabel>()
            .First(l => l.IsVisible);
        Assert.Equal(string.Format(Strings.Update_StatusAvailable, "999.0.0"), link.Text);
    }
}
