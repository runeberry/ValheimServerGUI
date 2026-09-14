using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ValheimServerGUI.App.Services;
using ValheimServerGUI.App.Tests.Fakes;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.App.Views.Dialogs;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Logging;
using Xunit;

namespace ValheimServerGUI.App.Tests.ViewModels;

/// <summary>
/// Profile re-targeting: switching a window onto a different profile's server swaps the event wiring, re-seeds
/// the status gates from the selected server (no event needed), and re-points the Logs buffer — while a
/// background server keeps running and no longer drives the window it was switched away from.
/// </summary>
public sealed class MainWindowViewModelRetargetTests : IDisposable
{
    private static readonly IServiceProvider Core =
        new ServiceCollection().AddValheimCore().BuildServiceProvider();

    private readonly string _dir;
    private readonly string _exe;
    private readonly string _saveDir;

    public MainWindowViewModelRetargetTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "vsg-retarget-" + Guid.NewGuid().ToString("N"));
        _saveDir = Path.Combine(_dir, "save");
        Directory.CreateDirectory(Path.Combine(_saveDir, "worlds"));
        _exe = Path.Combine(_dir, "valheim_server.x86_64");
        File.WriteAllText(_exe, "");
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private ValheimServerOptions Options() => new()
    {
        Name = "Srv",
        WorldName = "Worldxx",
        Password = "hunter2",
        Port = 2456,
        SaveInterval = 30,
        Backups = 1,
        BackupShort = 60,
        BackupLong = 120,
        ServerExePath = _exe,
        SaveDataFolderPath = _saveDir,
        LogToFile = false,
    };

    private (MainWindowViewModel Vm, ServerManager Manager, FakeServerPreferencesProvider ServerPrefs) Build(
        params string[] profiles)
    {
        var serverPrefs = new FakeServerPreferencesProvider(
            profiles.Select(p => new ServerPreferences { ProfileName = p }));
        var shell = new ShellLauncher(new Services.RecordingSystemShell(), TestLog.Silent);

        // Each server is built over its own headless process provider so it can be driven to Running.
        var manager = new ServerManager(
            () => new ValheimServer(
                new FakeProcessProvider(),
                Core.GetRequiredService<IPlayerDataRepository>(),
                Core.GetRequiredService<IApplicationLogger>(),
                Core.GetRequiredService<IValheimPathResolver>()),
            Core.GetRequiredService<Serilog.ILogger>());

        var vm = new MainWindowViewModel(
            manager,
            new FakeUserPreferencesProvider(),
            serverPrefs,
            Core.GetRequiredService<IWorldPreferencesProvider>(),
            new FakeSteamCloudWorldProvider(),
            Core.GetRequiredService<IIpAddressProvider>(),
            Core.GetRequiredService<IPlayerDataRepository>(),
            Core.GetRequiredService<IApplicationLogger>(),
            new FakeSoftwareUpdateProvider(),
            shell,
            Core.GetRequiredService<IValheimPathResolver>());

        return (vm, manager, serverPrefs);
    }

    private static void RunToRunning(ValheimServer server, ValheimServerOptions options)
    {
        server.Start(options);
        server.Logger!.Information("Game server connected"); // parser promotes Starting → Running
    }

    [Fact]
    public void Switching_targets_an_independent_server_per_profile()
    {
        var (vm, mgr, _) = Build("A", "B");

        vm.LoadProfile(new ServerPreferences { ProfileName = "A" });
        var serverA = vm.Server;

        vm.LoadProfile(new ServerPreferences { ProfileName = "B" });
        var serverB = vm.Server;

        Assert.NotNull(serverA);
        Assert.NotNull(serverB);
        Assert.NotSame(serverA, serverB);
        Assert.Equal(2, mgr.All.Count);
    }

    [Fact]
    public void Switching_to_a_running_profile_seeds_the_stop_gates_with_no_event()
    {
        var (vm, mgr, _) = Build("A", "B");
        vm.LoadProfile(new ServerPreferences { ProfileName = "A" });

        // Drive B to Running in the background, then switch the window onto it.
        RunToRunning(mgr.GetOrCreate("B"), Options());
        vm.LoadProfile(new ServerPreferences { ProfileName = "B" });

        Assert.Equal(ServerStatus.Running, vm.ServerStatus);
        Assert.True(vm.CanStop);
        Assert.True(vm.CanRestart);
        Assert.False(vm.AllowServerChanges); // fields locked while the selected server runs
    }

    [Fact]
    public void A_background_servers_status_change_does_not_drive_the_switched_away_window()
    {
        var (vm, mgr, _) = Build("A", "B");
        vm.LoadProfile(new ServerPreferences { ProfileName = "A" });
        var serverA = mgr.GetOrCreate("A");

        vm.LoadProfile(new ServerPreferences { ProfileName = "B" }); // switch away from A
        Assert.Equal(ServerStatus.Stopped, vm.ServerStatus);

        RunToRunning(serverA, Options()); // A now Running in the background
        Assert.Equal(ServerStatus.Running, serverA.Status);

        // The window still reflects B (stopped), not A — A's StatusChanged was unsubscribed on the switch.
        Assert.Equal(ServerStatus.Stopped, vm.ServerStatus);
        Assert.True(vm.CanStart);
    }

    [Fact]
    public void Switching_repoints_the_logs_buffer()
    {
        var (vm, mgr, _) = Build("A", "B");

        vm.LoadProfile(new ServerPreferences { ProfileName = "A" });
        mgr.GetLogAppender("A")("hello-A");
        Assert.Contains("hello-A", vm.Logs.CurrentLines);

        vm.LoadProfile(new ServerPreferences { ProfileName = "B" });
        Assert.DoesNotContain("hello-A", vm.Logs.CurrentLines); // now B's (empty) buffer
        mgr.GetLogAppender("B")("hello-B");
        Assert.Contains("hello-B", vm.Logs.CurrentLines);

        vm.LoadProfile(new ServerPreferences { ProfileName = "A" }); // back to A — its buffer survived
        Assert.Contains("hello-A", vm.Logs.CurrentLines);
    }

    [Fact]
    public async Task Clean_switch_does_not_prompt()
    {
        var (vm, _, _) = Build("A", "B");
        vm.LoadProfile(new ServerPreferences { ProfileName = "A" }); // load leaves the form clean

        var prompted = false;
        vm.UnsavedChangesPrompt = () => { prompted = true; return Task.FromResult(UnsavedChangesChoice.Cancel); };

        var ok = await vm.RequestSwitchProfileAsync("B");

        Assert.True(ok);
        Assert.False(prompted);
        Assert.Equal("B", vm.CurrentProfile?.ProfileName);
    }

    [Fact]
    public async Task Dirty_switch_cancel_aborts_and_keeps_the_current_profile()
    {
        var (vm, _, _) = Build("A", "B");
        vm.LoadProfile(new ServerPreferences { ProfileName = "A" });
        vm.Form.Name = "edited"; // dirty

        var prompted = 0;
        vm.UnsavedChangesPrompt = () => { prompted++; return Task.FromResult(UnsavedChangesChoice.Cancel); };

        var ok = await vm.RequestSwitchProfileAsync("B");

        Assert.False(ok);
        Assert.Equal(1, prompted);
        Assert.Equal("A", vm.CurrentProfile?.ProfileName); // stayed on A
    }

    [Fact]
    public async Task Dirty_switch_save_persists_then_switches()
    {
        var (vm, _, serverPrefs) = Build("A", "B");
        vm.LoadProfile(new ServerPreferences { ProfileName = "A" });
        vm.Form.Name = "editedA"; // dirty

        vm.UnsavedChangesPrompt = () => Task.FromResult(UnsavedChangesChoice.Save);

        var ok = await vm.RequestSwitchProfileAsync("B");

        Assert.True(ok);
        Assert.Equal("B", vm.CurrentProfile?.ProfileName);
        Assert.Equal("editedA", serverPrefs.LoadPreferences("A")?.Name); // A was saved before switching
    }

    [Fact]
    public async Task Switch_to_the_current_profile_is_a_no_op_without_prompting()
    {
        var (vm, _, _) = Build("A");
        vm.LoadProfile(new ServerPreferences { ProfileName = "A" });
        vm.Form.Name = "edited"; // dirty, but we're not actually switching

        var prompted = false;
        vm.UnsavedChangesPrompt = () => { prompted = true; return Task.FromResult(UnsavedChangesChoice.Cancel); };

        var ok = await vm.RequestSwitchProfileAsync("A");

        Assert.True(ok);
        Assert.False(prompted);
    }
}
