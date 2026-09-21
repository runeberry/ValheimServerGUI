using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ValheimServerGUI.App.Services;
using ValheimServerGUI.App.Tests.Fakes;
using ValheimServerGUI.App.Tests.Services;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.App.Views.Dialogs;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;
using Xunit;

namespace ValheimServerGUI.App.Tests.ViewModels;

// Player-list import (ad-hoc + first-launch) and the start-time conflict/permitted-file safety flow.
public sealed class PlayerListImportFlowTests : IDisposable
{
    private static readonly IServiceProvider Core =
        new ServiceCollection().AddValheimCore().BuildServiceProvider();

    private const string SteamA = "76561198000000001";
    private const string SteamB = "76561198000000002";

    private readonly string _dir;
    private readonly string _saveDir;
    private readonly string _exe;

    public PlayerListImportFlowTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "vsg-import-" + Guid.NewGuid().ToString("N"));
        _saveDir = Path.Combine(_dir, "save");
        Directory.CreateDirectory(Path.Combine(_saveDir, "worlds"));
        _exe = Path.Combine(_dir, "valheim_server.x86_64");
        File.WriteAllText(_exe, "");
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private sealed class Harness
    {
        public required MainWindowViewModel Vm { get; init; }
        public required FakeRuneberryApiClient Api { get; init; }
        public List<(string Title, string Body)> Messages { get; } = new();
        public List<string> ConfirmBodies { get; } = new();
        public List<string> ConflictBodies { get; } = new();
        public List<IValheimServerOptions> Started { get; } = new();
        public bool ConfirmResult { get; set; } = true;
        public RoleConflictChoice ConflictResult { get; set; } = RoleConflictChoice.UseServerProfile;
    }

    // A VM over the temp savedir. `withConfig` seeds a valid, startable form (world created + selected).
    private Harness Build(bool withConfig = false)
    {
        var shell = new ShellLauncher(new RecordingSystemShell(), TestLog.Silent);
        var manager = new ServerManager(
            () => Core.GetRequiredService<ValheimServer>(),
            Core.GetRequiredService<Serilog.ILogger>());
        var api = new FakeRuneberryApiClient();

        var vm = new MainWindowViewModel(
            manager,
            new FakeUserPreferencesProvider(),
            new FakeServerPreferencesProvider(),
            Core.GetRequiredService<IWorldPreferencesProvider>(),
            new FakeSteamCloudWorldProvider(),
            Core.GetRequiredService<IIpAddressProvider>(),
            Core.GetRequiredService<IPlayerDataRepository>(),
            Core.GetRequiredService<ValheimServerGUI.Tools.Logging.IApplicationLogger>(),
            new FakeSoftwareUpdateProvider(),
            shell,
            Core.GetRequiredService<IValheimPathResolver>(),
            Core.GetRequiredService<IPlayerListImportService>(),
            api);

        vm.LoadProfile(new ServerPreferences { ProfileName = "Test" });

        var h = new Harness { Vm = vm, Api = api };
        vm.MessagePrompt = (t, b) => { h.Messages.Add((t, b)); return Task.CompletedTask; };
        vm.ImportConfirmPrompt = b => { h.ConfirmBodies.Add(b); return Task.FromResult(h.ConfirmResult); };
        vm.ConflictPrompt = b => { h.ConflictBodies.Add(b); return Task.FromResult(h.ConflictResult); };
        vm.StartAction = h.Started.Add; // don't actually launch

        vm.Form.SaveDataFolderPath = _saveDir;
        vm.Form.ServerExePath = _exe;

        if (withConfig)
        {
            vm.Form.Name = "MyServer";
            vm.Form.Password = "hunter2";
            vm.Form.Port = FreeUdpPort();
            CreateLocalWorld("Existingworld");
            vm.RefreshWorldsCommand.Execute(null);
            vm.Form.UseNewWorld = false;
            vm.Form.ExistingWorld = "Existingworld";
        }

        return h;
    }

    private static int FreeUdpPort()
    {
        using var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        s.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)s.LocalEndPoint!).Port;
    }

    private void CreateLocalWorld(string name)
        => File.WriteAllText(Path.Combine(_saveDir, "worlds", name + ".fwl"), string.Empty);

    private void Seed(string fileName, params string[] entries)
        => File.WriteAllText(Path.Combine(_saveDir, fileName), "// header\n" + string.Join('\n', entries) + "\n");

    // ---- ad-hoc import ----

    [Fact]
    public async Task AdHoc_import_applies_roles_confirms_and_reports_updated()
    {
        var h = Build();
        Seed("adminlist.txt", SteamA);

        await h.Vm.RunImportAsync(interactive: true);

        Assert.Contains(MainWindowViewModel.ImportConfirmMessage.Replace("{n}", "1"), h.ConfirmBodies);
        Assert.Equal(PlayerRole.Admin, h.Vm.Form.GetRole($"Steam:{SteamA}"));
        Assert.True(h.Vm.Form.IsDirty);
        Assert.Contains(h.Messages, m => m.Body == MainWindowViewModel.ImportUpdatedMessage.Replace("{n}", "1"));
        Assert.Equal(1, h.Api.RequestPlayerInfoCallCount); // name lookup for the new player
    }

    [Fact]
    public async Task AdHoc_import_no_files_reports_no_files()
    {
        var h = Build();

        await h.Vm.RunImportAsync(interactive: true);

        Assert.Contains(h.Messages, m => m.Body == MainWindowViewModel.ImportNoFilesMessage);
        Assert.Empty(h.ConfirmBodies);
    }

    [Fact]
    public async Task AdHoc_import_no_changes_reports_no_roles()
    {
        var h = Build();
        Seed("adminlist.txt", SteamA);
        // Make the form already match the file so there is nothing to update.
        h.Vm.Form.SetRole(new PlayerInfo { Platform = "Steam", PlatformRaw = "Steam", PlayerId = SteamA }, PlayerRole.Admin);

        await h.Vm.RunImportAsync(interactive: true);

        Assert.Contains(h.Messages, m => m.Body == MainWindowViewModel.ImportNoRolesMessage);
        Assert.Empty(h.ConfirmBodies);
    }

    [Fact]
    public async Task AdHoc_import_unresolvable_token_reports_failure()
    {
        var h = Build();
        Seed("adminlist.txt", "totally-bogus");

        await h.Vm.RunImportAsync(interactive: true);

        Assert.Contains(h.Messages, m => m.Body == MainWindowViewModel.ImportFailedMessage);
    }

    [Fact]
    public async Task AdHoc_import_cancel_does_not_apply()
    {
        var h = Build();
        h.ConfirmResult = false;
        Seed("adminlist.txt", SteamA);

        await h.Vm.RunImportAsync(interactive: true);

        Assert.Null(h.Vm.Form.GetRole($"Steam:{SteamA}"));
        Assert.DoesNotContain(h.Messages, m => m.Body.StartsWith("Updated"));
    }

    // ---- first-launch auto-import ----

    [Fact]
    public void First_launch_auto_import_runs_silently_when_no_roles()
    {
        var h = Build();
        Seed("adminlist.txt", SteamA);

        h.Vm.LoadProfile(new ServerPreferences { ProfileName = "Fresh", SaveDataFolderPath = _saveDir });

        Assert.Equal(PlayerRole.Admin, h.Vm.Form.GetRole($"Steam:{SteamA}"));
        Assert.Empty(h.Messages);       // silent — no modals
        Assert.Empty(h.ConfirmBodies);
    }

    [Fact]
    public void First_launch_auto_import_skipped_when_profile_already_has_roles()
    {
        var h = Build();
        Seed("adminlist.txt", SteamA);

        var profile = new ServerPreferences { ProfileName = "Has", SaveDataFolderPath = _saveDir };
        profile.PlayerRoles[$"Steam:{SteamB}"] = new PlayerRoleEntry(PlayerRole.Permitted, "Steam");
        h.Vm.LoadProfile(profile);

        Assert.Null(h.Vm.Form.GetRole($"Steam:{SteamA}"));                  // file NOT imported
        Assert.Equal(PlayerRole.Permitted, h.Vm.Form.GetRole($"Steam:{SteamB}")); // only the profile role
    }

    // ---- start-time safety ----

    [Fact]
    public async Task Start_open_mode_backs_up_stray_permitted_list()
    {
        var h = Build(withConfig: true);
        Seed("permittedlist.txt", SteamA); // open mode (UsePermittedList=false) + a non-empty permitted list

        await h.Vm.StartServerAsync(isManual: true);

        Assert.Single(h.Started);
        Assert.False(File.Exists(Path.Combine(_saveDir, "permittedlist.txt")));
        Assert.True(File.Exists(Path.Combine(_saveDir, "permittedlist.bak.txt")));
    }

    [Fact]
    public async Task Start_permitted_file_that_cannot_be_moved_aborts_with_error()
    {
        var h = Build(withConfig: true);
        Seed("permittedlist.txt", SteamA);
        // Occupy the backup destination with a directory so the move fails.
        Directory.CreateDirectory(Path.Combine(_saveDir, "permittedlist.bak.txt"));

        await h.Vm.StartServerAsync(isManual: true);

        Assert.Empty(h.Started); // aborted
        Assert.Contains(h.Messages, m => m.Title == MainWindowViewModel.PermittedFileErrorTitle);
    }

    [Fact]
    public async Task Start_conflict_use_roles_from_file_sets_skip_generation()
    {
        var h = Build(withConfig: true);
        h.ConflictResult = RoleConflictChoice.UseRolesFromFile;
        // Config says banned, adminlist says admin → conflict.
        h.Vm.Form.SetRole(new PlayerInfo { Platform = "Steam", PlatformRaw = "Steam", PlayerId = SteamA }, PlayerRole.Banned);
        Seed("adminlist.txt", SteamA);

        await h.Vm.StartServerAsync(isManual: true);

        var opts = Assert.Single(h.Started);
        Assert.True(opts.SkipAccessListGeneration);
        Assert.Single(h.ConflictBodies);
    }

    [Fact]
    public async Task Start_conflict_cancel_aborts_start()
    {
        var h = Build(withConfig: true);
        h.ConflictResult = RoleConflictChoice.Cancel;
        h.Vm.Form.SetRole(new PlayerInfo { Platform = "Steam", PlatformRaw = "Steam", PlayerId = SteamA }, PlayerRole.Banned);
        Seed("adminlist.txt", SteamA);

        await h.Vm.StartServerAsync(isManual: true);

        Assert.Empty(h.Started);
    }

    [Fact]
    public async Task Start_conflict_autostart_defaults_to_profile_and_backs_up()
    {
        var h = Build(withConfig: true);
        h.Vm.Form.SetRole(new PlayerInfo { Platform = "Steam", PlatformRaw = "Steam", PlayerId = SteamA }, PlayerRole.Banned);
        Seed("adminlist.txt", SteamA);

        await h.Vm.StartServerAsync(isManual: false); // auto-start: no prompt, profile wins

        var opts = Assert.Single(h.Started);
        Assert.False(opts.SkipAccessListGeneration);                              // profile wins → generate
        Assert.Empty(h.ConflictBodies);                                          // prompt never consulted
        Assert.True(File.Exists(Path.Combine(_saveDir, "adminlist.bak.txt")));   // conflicting file backed up
    }
}
