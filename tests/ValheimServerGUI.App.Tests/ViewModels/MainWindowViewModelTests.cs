using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using Microsoft.Extensions.DependencyInjection;
using ValheimServerGUI.App.Converters;
using ValheimServerGUI.App.Services;
using ValheimServerGUI.App.Tests.Fakes;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.Game;
using ValheimServerGUI.Localization;
using ValheimServerGUI.Tools;
using Xunit;

namespace ValheimServerGUI.App.Tests.ViewModels;

public class MainWindowViewModelTests
{
    private static readonly IServiceProvider Core =
        new ServiceCollection().AddValheimCore().BuildServiceProvider();

    private static MainWindowViewModel Build(
        out FakeSoftwareUpdateProvider update,
        IEnumerable<ServerPreferences>? profiles = null)
    {
        update = new FakeSoftwareUpdateProvider();
        return Build(update, profiles);
    }

    // A VM over saved profiles with these names, plus the provider so a test can remove one.
    private static MainWindowViewModel BuildWithProfiles(out FakeServerPreferencesProvider serverPrefs, params string[] names)
    {
        serverPrefs = new FakeServerPreferencesProvider(names.Select(n => new ServerPreferences { ProfileName = n }));
        return Build(new FakeSoftwareUpdateProvider(), serverPrefs: serverPrefs);
    }

    private static MainWindowViewModel Build(
        FakeSoftwareUpdateProvider update,
        IEnumerable<ServerPreferences>? profiles = null,
        Services.RecordingSystemShell? systemShell = null,
        FakeServerPreferencesProvider? serverPrefs = null)
    {
        var shell = new ShellLauncher(systemShell ?? new Services.RecordingSystemShell(), TestLog.Silent, isWindows: false);
        // A fresh manager per VM keeps servers isolated between tests (the DI provider is shared/static).
        var manager = new ServerManager(
            () => Core.GetRequiredService<ValheimServer>(),
            Core.GetRequiredService<Serilog.ILogger>());
        return new MainWindowViewModel(
            manager,
            new FakeUserPreferencesProvider(),
            serverPrefs ?? new FakeServerPreferencesProvider(profiles),
            Core.GetRequiredService<IWorldPreferencesProvider>(),
            Core.GetRequiredService<ISteamCloudWorldProvider>(),
            Core.GetRequiredService<IIpAddressProvider>(),
            Core.GetRequiredService<IPlayerDataRepository>(),
            Core.GetRequiredService<ValheimServerGUI.Tools.Logging.IApplicationLogger>(),
            update,
            shell,
            Core.GetRequiredService<IValheimPathResolver>(),
            Core.GetRequiredService<IPlayerListImportService>());
    }

    [Fact]
    public void Stopped_enables_start_and_editing_only()
    {
        var vm = Build(out _);
        vm.ServerStatus = ServerStatus.Stopped;

        Assert.True(vm.CanStart);
        Assert.False(vm.CanStop);
        Assert.False(vm.CanRestart);
        Assert.True(vm.AllowServerChanges);
        Assert.True(vm.StartCommand.CanExecute(null));
        Assert.False(vm.StopCommand.CanExecute(null));
        Assert.True(vm.NewProfileCommand.CanExecute(null)); // ungated (allowed regardless of state)
    }

    [Fact]
    public void Running_enables_stop_restart_and_locks_editing()
    {
        var vm = Build(out _);
        vm.ServerStatus = ServerStatus.Running;

        Assert.False(vm.CanStart);
        Assert.True(vm.CanStop);
        Assert.True(vm.CanRestart);
        Assert.False(vm.AllowServerChanges);
        Assert.False(vm.StartCommand.CanExecute(null));
        Assert.True(vm.StopCommand.CanExecute(null));
        Assert.True(vm.RestartCommand.CanExecute(null));
        Assert.True(vm.NewProfileCommand.CanExecute(null)); // ungated: profile mgmt allowed while running
    }

    [Theory]
    [InlineData(ServerStatus.Starting, false, true, false)]
    [InlineData(ServerStatus.Stopping, false, false, false)]
    public void Transitional_states_gate_correctly(ServerStatus status, bool canStart, bool canStop, bool canRestart)
    {
        var vm = Build(out _);
        vm.ServerStatus = status;

        Assert.Equal(canStart, vm.CanStart);
        Assert.Equal(canStop, vm.CanStop);
        Assert.Equal(canRestart, vm.CanRestart);
        Assert.Equal(EnumDisplayConverter.ToText(status), vm.StatusText);
    }

    [Fact]
    public void Status_change_raises_command_can_execute_changed()
    {
        var vm = Build(out _);
        vm.ServerStatus = ServerStatus.Stopped;

        var raised = false;
        vm.StopCommand.CanExecuteChanged += (_, _) => raised = true;
        vm.ServerStatus = ServerStatus.Running;

        Assert.True(raised);
        Assert.True(vm.StopCommand.CanExecute(null));
    }

    [Fact]
    public void Load_profile_command_is_ungated_by_server_state()
    {
        // Switching profiles must work while a server is running (the switch re-targets the window).
        var vm = Build(out _, profiles: new[] { new ServerPreferences { ProfileName = "A" } });
        vm.ServerStatus = ServerStatus.Stopped;
        Assert.True(vm.LoadProfileCommand.CanExecute("A"));
        Assert.True(vm.HasProfiles);

        vm.ServerStatus = ServerStatus.Running;
        Assert.True(vm.LoadProfileCommand.CanExecute("A"));
    }

    [Fact]
    public void Profile_commands_tolerate_a_non_string_parameter()
    {
        // The dynamic Load/Remove Profile submenus bind CommandParameter via {Binding}, which transiently
        // resolves to the inherited (VM) DataContext while a generated item's container is set up. A
        // string-typed RelayCommand would throw on that argument in CanExecute and crash the File menu on
        // open — so the commands must accept any parameter type.
        var vm = Build(out _);

        Assert.Null(Record.Exception(() => vm.LoadProfileCommand.CanExecute(vm)));
        Assert.Null(Record.Exception(() => vm.RemoveProfileCommand.CanExecute(vm)));
        // A non-string execute is a harmless no-op (no switch, no removal request raised).
        var removed = false;
        vm.RemoveProfileRequested += _ => removed = true;
        vm.RemoveProfileCommand.Execute(vm);
        Assert.False(removed);
    }

    // Removal arrives through the provider's PreferencesSaved event, which the VM handles on the UI thread.
    [AvaloniaFact]
    public void Removing_the_active_profile_switches_to_Default()
    {
        var vm = BuildWithProfiles(out var prefs, "Alpha", "Default", "Zed");
        vm.LoadProfile(prefs.LoadPreferences("Zed")!);

        prefs.RemovePreferences("Zed");

        Assert.Equal("Default", vm.CurrentProfile?.ProfileName);
    }

    [AvaloniaFact]
    public void Removing_the_active_profile_without_Default_switches_to_the_first_profile()
    {
        var vm = BuildWithProfiles(out var prefs, "Charlie", "Bravo", "Alpha");
        vm.LoadProfile(prefs.LoadPreferences("Charlie")!);

        prefs.RemovePreferences("Charlie");

        Assert.Equal("Alpha", vm.CurrentProfile?.ProfileName);
    }

    [AvaloniaFact]
    public void Removing_another_profile_keeps_the_current_one()
    {
        var vm = BuildWithProfiles(out var prefs, "Alpha", "Bravo", "Default");
        vm.LoadProfile(prefs.LoadPreferences("Alpha")!);

        prefs.RemovePreferences("Bravo");

        Assert.Equal("Alpha", vm.CurrentProfile?.ProfileName);
        Assert.Equal(new[] { "Alpha", "Default" }, vm.Profiles);
    }

    [AvaloniaFact]
    public void The_last_profile_cannot_be_removed()
    {
        var vm = BuildWithProfiles(out var prefs, "Alpha", "Bravo");
        var requested = new List<string>();
        vm.RemoveProfileRequested += requested.Add;
        Assert.True(vm.CanRemoveProfile);

        prefs.RemovePreferences("Bravo");
        vm.RemoveProfileCommand.Execute("Alpha");

        Assert.False(vm.CanRemoveProfile);
        Assert.Empty(requested);
    }

    private const string ReleasePage = "https://forge.example/releases/tag/v9.9.9";

    // The URLs the shell was asked to open (xdg-open's argument; the VM is built with isWindows: false).
    private static IEnumerable<string> Opened(Services.RecordingSystemShell shell) => shell.Started.SelectMany(s => s.Args);

    [Fact]
    public void Update_available_links_to_the_returned_release_page()
    {
        var update = new FakeSoftwareUpdateProvider();
        var shell = new Services.RecordingSystemShell();
        var vm = Build(update, systemShell: shell);
        update.RaiseFinished(new SoftwareUpdateEventArgs("9.9.9", ReleasePage, isManualCheck: false));

        Assert.Equal(string.Format(Strings.Update_StatusAvailable, "9.9.9"), vm.UpdateStatusText);
        Assert.True(vm.UpdateIsLink);
        vm.UpdateLinkCommand.Execute(null);
        Assert.Equal(new[] { ReleasePage }, Opened(shell));
    }

    [Fact]
    public void Update_failure_is_not_a_link()
    {
        var vm = Build(out var update);
        update.RaiseFinished(new SoftwareUpdateEventArgs(new Exception("no net"), isManualCheck: false));

        Assert.Equal(Strings.Update_StatusFailed, vm.UpdateStatusText);
        Assert.False(vm.UpdateIsLink);
        Assert.False(vm.UpdateLinkCommand.CanExecute(null));
    }

    [Fact]
    public void Manual_check_with_a_release_page_offers_to_open_it()
    {
        var update = new FakeSoftwareUpdateProvider();
        var shell = new Services.RecordingSystemShell();
        var vm = Build(update, systemShell: shell);
        string? asked = null;
        vm.UpdateResultPrompt = body => { asked = body; return Task.FromResult(true); };

        update.RaiseFinished(new SoftwareUpdateEventArgs("9.9.9", ReleasePage, isManualCheck: true));

        Assert.Equal(string.Format(Strings.Update_ManualPrompt, Strings.Update_ManualAvailable), asked);
        Assert.Equal(new[] { ReleasePage }, Opened(shell));
    }

    [Fact]
    public void Manual_check_without_a_release_page_only_reports()
    {
        var update = new FakeSoftwareUpdateProvider();
        var shell = new Services.RecordingSystemShell();
        var vm = Build(update, systemShell: shell);
        var asked = false;
        vm.UpdateResultPrompt = _ => { asked = true; return Task.FromResult(true); };
        (string Title, string Body)? shown = null;
        vm.MessagePrompt = (title, body) => { shown = (title, body); return Task.CompletedTask; };

        update.RaiseFinished(new SoftwareUpdateEventArgs(new Exception("no net"), isManualCheck: true));

        Assert.False(asked);
        Assert.Equal((Strings.Prompt_CheckForUpdates_Title, string.Format(Strings.Update_ManualFailed, "no net")), shown);
        Assert.Empty(Opened(shell));
    }

    [Fact]
    public void Up_to_date_shows_the_version_and_is_not_a_link()
    {
        var vm = Build(out var update);
        var current = AssemblyHelper.GetApplicationVersion();
        update.RaiseFinished(new SoftwareUpdateEventArgs(current, ReleasePage, isManualCheck: false));

        Assert.Equal(string.Format(Strings.Update_StatusUpToDate, current), vm.UpdateStatusText); // version shown, WinForms-style
        Assert.Equal(UpdateCheckStatus.UpToDate, vm.UpdateStatus);
        Assert.False(vm.UpdateIsLink);
        Assert.False(vm.UpdateLinkCommand.CanExecute(null));
    }

    [Fact]
    public void Older_latest_than_current_is_a_pre_release_build()
    {
        var vm = Build(out var update);
        // A latest older than the running (pre-release) build → "Pre-release build (<current>)".
        update.RaiseFinished(new SoftwareUpdateEventArgs("0.0.1", ReleasePage, isManualCheck: false));

        Assert.Equal(string.Format(Strings.Update_StatusPreRelease, AssemblyHelper.GetApplicationVersion()), vm.UpdateStatusText);
        Assert.Equal(UpdateCheckStatus.PreRelease, vm.UpdateStatus);
        Assert.False(vm.UpdateIsLink);
    }

    [Fact]
    public void Unparseable_latest_version_is_an_error_link()
    {
        var vm = Build(out var update);
        update.RaiseFinished(new SoftwareUpdateEventArgs("not-a-version", ReleasePage, isManualCheck: false));

        Assert.Equal(string.Format(Strings.Update_StatusUnparsable, "not-a-version"), vm.UpdateStatusText);
        Assert.Equal(UpdateCheckStatus.Error, vm.UpdateStatus);
        Assert.True(vm.UpdateIsLink);
    }

    [Fact]
    public void Readout_is_seeded_from_the_startup_check_result()
    {
        // The startup check completes before this window's VM exists; a VM built afterwards must still
        // reflect it from LastResult (not sit blank).
        var update = new FakeSoftwareUpdateProvider();
        update.RaiseFinished(new SoftwareUpdateEventArgs("9.9.9", ReleasePage, isManualCheck: false)); // sets LastResult

        var vm = Build(update);
        Assert.Equal(string.Format(Strings.Update_StatusAvailable, "9.9.9"), vm.UpdateStatusText);
        Assert.Equal(UpdateCheckStatus.Available, vm.UpdateStatus);
    }

    [Fact]
    public void Title_derives_from_current_profile()
    {
        var vm = Build(out _);
        Assert.Equal("(Unofficial) Valheim Dedicated Server GUI", vm.Title);

        vm.LoadProfile(new ServerPreferences { ProfileName = "Nightshade" });
        Assert.Equal("(Unofficial) Valheim Dedicated Server GUI — Nightshade", vm.Title);
    }

    [Fact]
    public void CanSelectExistingWorld_requires_stopped_and_at_least_one_world()
    {
        var vm = Build(out _);
        vm.ServerStatus = ServerStatus.Stopped;
        Assert.False(vm.CanSelectExistingWorld); // no worlds -> disabled empty state

        vm.Form.Worlds.Add("Alpha");
        Assert.True(vm.CanSelectExistingWorld); // worlds exist and stopped

        vm.ServerStatus = ServerStatus.Running;
        Assert.False(vm.CanSelectExistingWorld); // locked while running
    }
}
