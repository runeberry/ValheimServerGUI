using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using ValheimServerGUI.App.Services;
using ValheimServerGUI.App.Tests.Fakes;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.Game;
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

    private static MainWindowViewModel Build(
        FakeSoftwareUpdateProvider update,
        IEnumerable<ServerPreferences>? profiles = null)
    {
        var shell = new ShellLauncher(new Services.RecordingSystemShell(), TestLog.Silent);
        return new MainWindowViewModel(
            Core.GetRequiredService<ValheimServer>(),
            new FakeUserPreferencesProvider(),
            new FakeServerPreferencesProvider(profiles),
            Core.GetRequiredService<IWorldPreferencesProvider>(),
            Core.GetRequiredService<ISteamCloudWorldProvider>(),
            Core.GetRequiredService<IIpAddressProvider>(),
            Core.GetRequiredService<IPlayerDataRepository>(),
            Core.GetRequiredService<ValheimServerGUI.Tools.Logging.IApplicationLogger>(),
            update,
            shell,
            Core.GetRequiredService<IValheimPathResolver>());
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
        Assert.True(vm.NewProfileCommand.CanExecute(null)); // Stopped-only menu item enabled
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
        Assert.False(vm.NewProfileCommand.CanExecute(null)); // gated while running
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
        Assert.Equal("Stopping".Equals(status.ToString()) ? "Stopping" : "Starting", vm.StatusText);
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
    public void Load_profile_command_gated_by_stopped_state()
    {
        var vm = Build(out _, profiles: new[] { new ServerPreferences { ProfileName = "A" } });
        vm.ServerStatus = ServerStatus.Stopped;
        Assert.True(vm.LoadProfileCommand.CanExecute("A"));
        Assert.True(vm.HasProfiles);

        vm.ServerStatus = ServerStatus.Running;
        Assert.False(vm.LoadProfileCommand.CanExecute("A"));
    }

    [Fact]
    public void Update_available_becomes_a_link()
    {
        var vm = Build(out var update);
        update.RaiseFinished(new SoftwareUpdateEventArgs("9.9.9", isManualCheck: true));

        Assert.Contains("9.9.9", vm.UpdateStatusText);
        Assert.True(vm.UpdateIsLink);
        Assert.True(vm.UpdateLinkCommand.CanExecute(null));
    }

    [Fact]
    public void Update_failure_becomes_a_link()
    {
        var vm = Build(out var update);
        update.RaiseFinished(new SoftwareUpdateEventArgs(new Exception("no net"), isManualCheck: true));

        Assert.Contains("failed", vm.UpdateStatusText, StringComparison.OrdinalIgnoreCase);
        Assert.True(vm.UpdateIsLink);
    }

    [Fact]
    public void Up_to_date_shows_the_version_and_is_not_a_link()
    {
        var vm = Build(out var update);
        var current = AssemblyHelper.GetApplicationVersion();
        update.RaiseFinished(new SoftwareUpdateEventArgs(current, isManualCheck: true));

        Assert.Equal($"Up to date ({current})", vm.UpdateStatusText); // version shown, WinForms-style
        Assert.Equal(UpdateCheckStatus.UpToDate, vm.UpdateStatus);
        Assert.False(vm.UpdateIsLink);
        Assert.False(vm.UpdateLinkCommand.CanExecute(null));
    }

    [Fact]
    public void Older_latest_than_current_is_a_pre_release_build()
    {
        var vm = Build(out var update);
        // A latest older than the running (pre-release) build → "Pre-release build (<current>)".
        update.RaiseFinished(new SoftwareUpdateEventArgs("0.0.1", isManualCheck: true));

        Assert.StartsWith("Pre-release build", vm.UpdateStatusText);
        Assert.Contains(AssemblyHelper.GetApplicationVersion(), vm.UpdateStatusText);
        Assert.Equal(UpdateCheckStatus.PreRelease, vm.UpdateStatus);
        Assert.False(vm.UpdateIsLink);
    }

    [Fact]
    public void Unparseable_latest_version_is_an_error_link()
    {
        var vm = Build(out var update);
        update.RaiseFinished(new SoftwareUpdateEventArgs("not-a-version", isManualCheck: true));

        Assert.Contains("Unable to parse", vm.UpdateStatusText);
        Assert.Equal(UpdateCheckStatus.Error, vm.UpdateStatus);
        Assert.True(vm.UpdateIsLink);
    }

    [Fact]
    public void Readout_is_seeded_from_the_startup_check_result()
    {
        // The startup check completes before this window's VM exists; a VM built afterwards must still
        // reflect it from LastResult (not sit blank).
        var update = new FakeSoftwareUpdateProvider();
        update.RaiseFinished(new SoftwareUpdateEventArgs("9.9.9", isManualCheck: false)); // sets LastResult

        var vm = Build(update);
        Assert.Contains("9.9.9", vm.UpdateStatusText);
        Assert.Equal(UpdateCheckStatus.Available, vm.UpdateStatus);
    }

    [Fact]
    public void Title_derives_from_current_profile()
    {
        var vm = Build(out _);
        Assert.Equal("Valheim Server GUI", vm.Title);

        vm.LoadProfile(new ServerPreferences { ProfileName = "Nightshade" });
        Assert.Equal("Valheim Server GUI — Nightshade", vm.Title);
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
