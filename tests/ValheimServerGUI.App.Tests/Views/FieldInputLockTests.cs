using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using ValheimServerGUI.App.Controls;
using ValheimServerGUI.App.Services;
using ValheimServerGUI.App.Tests.Fakes;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.App.Views.Tabs;
using ValheimServerGUI.Game;
using ValheimServerGUI.Localization;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Logging;
using Xunit;

namespace ValheimServerGUI.App.Tests.Views;

// IsInputEnabled locks a field's input while leaving its read-only end-caps usable: a running server's password
// can still be shown and copied, and its folders can still be opened.
public class FieldInputLockTests
{
    private static readonly System.IServiceProvider Core =
        new ServiceCollection().AddValheimCore().BuildServiceProvider();

    private static T Show<T>(T content) where T : Control
    {
        var window = new Window { Content = content, Width = 700, Height = 600 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return content;
    }

    private static TextBox Input(Control field) => field.GetVisualDescendants().OfType<TextBox>().First(t => t.IsVisible);

    private static Button Browse(Control field)
        => field.GetVisualDescendants().OfType<Button>().Single(b => (b.Content as string) == Strings.Controls_Browse);

    private static Button OpenFolder(Control field)
        => field.GetVisualDescendants().OfType<Button>().Single(b => (ToolTip.GetTip(b) as string) == Strings.Controls_OpenThisFolder_Tip);

    [AvaloniaFact]
    public void TextFormField_input_lock_leaves_the_affix_usable()
    {
        var field = Show(new TextFormField
        {
            LabelText = "Password",
            IsInputEnabled = false,
            Affix = new Button { Classes = { "affix" }, Content = "Copy" },
        });

        Assert.False(Input(field).IsEffectivelyEnabled);
        Assert.True(field.GetVisualDescendants().OfType<Button>().Single().IsEffectivelyEnabled);
    }

    [AvaloniaFact]
    public void FilenameFormField_input_lock_disables_browse_but_not_open_folder()
    {
        var field = Show(new FilenameFormField { LabelText = "Path", ShowOpenFolder = true, IsInputEnabled = false });

        Assert.False(Input(field).IsEffectivelyEnabled);
        Assert.False(Browse(field).IsEffectivelyEnabled);
        Assert.True(OpenFolder(field).IsEffectivelyEnabled);
    }

    private static MainWindowViewModel BuildViewModel()
    {
        var shell = new ShellLauncher(new ValheimServerGUI.App.Tests.Services.RecordingSystemShell(), TestLog.Silent);
        var vm = new MainWindowViewModel(
            Core.GetRequiredService<IServerManager>(), new FakeUserPreferencesProvider(),
            new FakeServerPreferencesProvider(), Core.GetRequiredService<IWorldPreferencesProvider>(),
            Core.GetRequiredService<ISteamCloudWorldProvider>(), Core.GetRequiredService<IIpAddressProvider>(),
            Core.GetRequiredService<IPlayerDataRepository>(), Core.GetRequiredService<IApplicationLogger>(),
            new FakeSoftwareUpdateProvider(), shell,
            Core.GetRequiredService<IValheimPathResolver>(), Core.GetRequiredService<IPlayerListImportService>());
        vm.LoadProfile(new ServerPreferences { ProfileName = "Lock" });
        return vm;
    }

    [AvaloniaFact]
    public void Running_server_locks_the_password_input_but_not_show_or_copy()
    {
        var vm = BuildViewModel();
        var view = Show(new ServerControlsView { DataContext = vm });
        var password = view.GetVisualDescendants().OfType<TextFormField>()
            .Single(f => f.LabelText == Strings.ServerControls_Password_Label);
        var buttons = password.GetVisualDescendants().OfType<Button>().ToList();
        Assert.Equal(2, buttons.Count); // show/hide + copy

        vm.ServerStatus = ServerStatus.Running;
        Dispatcher.UIThread.RunJobs();

        Assert.False(Input(password).IsEffectivelyEnabled);
        Assert.All(buttons, b => Assert.True(b.IsEffectivelyEnabled));

        vm.ServerStatus = ServerStatus.Stopped;
        Dispatcher.UIThread.RunJobs();

        Assert.True(Input(password).IsEffectivelyEnabled);
    }

    [AvaloniaFact]
    public void Running_server_locks_directory_inputs_and_browse_but_not_open_folder()
    {
        var vm = BuildViewModel();
        var view = Show(new AdvancedControlsView { DataContext = vm });
        var paths = view.GetVisualDescendants().OfType<FilenameFormField>().ToList();
        Assert.Equal(2, paths.Count); // server exe + save data

        vm.ServerStatus = ServerStatus.Running;
        Dispatcher.UIThread.RunJobs();

        Assert.All(paths, f =>
        {
            Assert.False(Input(f).IsEffectivelyEnabled);
            Assert.False(Browse(f).IsEffectivelyEnabled);
            Assert.True(OpenFolder(f).IsEffectivelyEnabled);
        });

        vm.ServerStatus = ServerStatus.Stopped;
        Dispatcher.UIThread.RunJobs();

        Assert.All(paths, f =>
        {
            Assert.True(Input(f).IsEffectivelyEnabled);
            Assert.True(Browse(f).IsEffectivelyEnabled);
        });
    }
}
