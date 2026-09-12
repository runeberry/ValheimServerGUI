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
        var shell = new ShellLauncher(new Services.RecordingSystemShell(), TestLog.Silent);
        return new MainWindowViewModel(
            Core.GetRequiredService<ValheimServer>(),
            new FakeUserPreferencesProvider(),
            new FakeServerPreferencesProvider(profiles),
            Core.GetRequiredService<IWorldPreferencesProvider>(),
            Core.GetRequiredService<ISteamCloudWorldProvider>(),
            Core.GetRequiredService<IIpAddressProvider>(),
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
    public void Up_to_date_is_not_a_link()
    {
        var vm = Build(out var update);
        // The test host reports version "0.0.0"; anything not-newer is "up to date".
        update.RaiseFinished(new SoftwareUpdateEventArgs("0.0.0", isManualCheck: true));

        Assert.False(vm.UpdateIsLink);
        Assert.False(vm.UpdateLinkCommand.CanExecute(null));
    }

    [Fact]
    public void Title_derives_from_current_profile()
    {
        var vm = Build(out _);
        Assert.Equal("Valheim Server GUI", vm.Title);

        vm.LoadProfile(new ServerPreferences { ProfileName = "Nightshade" });
        Assert.Equal("Valheim Server GUI — Nightshade", vm.Title);
    }
}
