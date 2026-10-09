using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
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
/// The list-file safety invariant: VSG never erases an adminlist/bannedlist/permittedlist entry it did not write,
/// unless the file is backed up first. Every write path (start, restart, live role edit, defaults save, import,
/// each conflict choice) runs against real <see cref="ValheimServer"/>s over a headless process provider, and each
/// test asserts that every entry present before the action is still in a list file or a backup afterwards — except
/// entries the test deliberately removed through VSG. Entries appended mid-session stand in for the game's own
/// in-game ban/permit writes (or a hand edit, which is indistinguishable and treated the same).
/// </summary>
public sealed class PlayerListSafetyTests : IDisposable
{
    private static readonly IServiceProvider Core =
        new ServiceCollection().AddValheimCore().BuildServiceProvider();

    private const string SteamA = "76561198000000001";
    private const string SteamB = "76561198000000002";
    private const string SteamC = "76561198000000003";
    private const string Unrecognized = "PlayFab_1A2B3C4D5E6F";

    private const string Admin = "adminlist.txt";
    private const string Banned = "bannedlist.txt";
    private const string Permitted = "permittedlist.txt";

    private readonly string _dir;
    private readonly string _saveDir;
    private readonly string _exe;
    private readonly FakeUserPreferencesProvider _userPrefs = new();
    private readonly FakeServerPreferencesProvider _serverPrefs = new();
    private readonly List<FakeProcessProvider> _processes = new();

    public PlayerListSafetyTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "vsg-listsafety-" + Guid.NewGuid().ToString("N"));
        _saveDir = Path.Combine(_dir, "save");
        Directory.CreateDirectory(Path.Combine(_saveDir, "worlds"));
        File.WriteAllText(Path.Combine(_saveDir, "worlds", "Existingworld.fwl"), string.Empty);
        _exe = Path.Combine(_dir, "valheim_server.x86_64");
        File.WriteAllText(_exe, "");
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    // ===== Harness =====

    private sealed class Harness
    {
        public required MainWindowViewModel Vm { get; init; }
        public List<string> ConflictBodies { get; } = new();
        public List<(string Title, string Body)> Messages { get; } = new();
        public RoleConflictChoice ConflictResult { get; set; } = RoleConflictChoice.UseServerProfile;
    }

    private Harness Build(ServerPreferences? profile = null)
    {
        var shell = new ShellLauncher(new Services.RecordingSystemShell(), TestLog.Silent);
        // One baseline store per test, shared by the view-model's reconcile and the servers' writes.
        var baselines = new InMemoryBaselineStore();
        var accessLists = Core.GetRequiredService<IPlayerAccessListService>();
        var import = new PlayerListImportService(accessLists, baselines);
        var manager = new ServerManager(
            () =>
            {
                var processes = new FakeProcessProvider();
                _processes.Add(processes);
                return new ValheimServer(
                    processes,
                    Core.GetRequiredService<IPlayerDataRepository>(),
                    Core.GetRequiredService<IApplicationLogger>(),
                    Core.GetRequiredService<IValheimPathResolver>(),
                    accessLists, import, baselines);
            },
            Core.GetRequiredService<Serilog.ILogger>());

        var vm = new MainWindowViewModel(
            manager, _userPrefs, _serverPrefs,
            Core.GetRequiredService<IWorldPreferencesProvider>(), new FakeSteamCloudWorldProvider(),
            Core.GetRequiredService<IIpAddressProvider>(), Core.GetRequiredService<IPlayerDataRepository>(),
            Core.GetRequiredService<IApplicationLogger>(), new FakeSoftwareUpdateProvider(), shell,
            Core.GetRequiredService<IValheimPathResolver>(), import, new FakeRuneberryApiClient());

        var h = new Harness { Vm = vm };
        vm.MessagePrompt = (t, b) => { h.Messages.Add((t, b)); return Task.CompletedTask; };
        vm.ImportConfirmPrompt = _ => Task.FromResult(true);
        vm.ConflictPrompt = b => { h.ConflictBodies.Add(b); return Task.FromResult(h.ConflictResult); };

        profile ??= new ServerPreferences { ProfileName = "Safety" };
        profile.Name = "MyServer";
        profile.Password = "hunter2";
        profile.WorldName = "Existingworld";
        profile.Port = FreeUdpPort();
        profile.ServerExePath = _exe;
        profile.SaveDataFolderPath = _saveDir;
        _serverPrefs.SavePreferences(profile);
        vm.LoadProfile(profile);
        vm.RefreshWorldsCommand.Execute(null);
        vm.Form.UseNewWorld = false;
        vm.Form.ExistingWorld = "Existingworld";
        return h;
    }

    private static ServerPreferences ProfileWith(params (string id, PlayerRole role)[] overrides)
    {
        var profile = new ServerPreferences { ProfileName = "Safety" };
        foreach (var (id, role) in overrides)
            profile.PlayerRoles[$"Steam:{id}"] = new PlayerRoleEntry(role, "Steam");
        return profile;
    }

    private void SaveDefaults(params (string id, PlayerRole role)[] entries)
    {
        var prefs = _userPrefs.LoadPreferences();
        foreach (var (id, role) in entries)
            prefs.PlayerDefaults[$"Steam:{id}"] = new PlayerDefaultEntry(role, "Steam");
        _userPrefs.SavePreferences(prefs);
    }

    private static int FreeUdpPort()
    {
        using var s = new System.Net.Sockets.Socket(
            System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
        s.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 0));
        return ((System.Net.IPEndPoint)s.LocalEndPoint!).Port;
    }

    // Starts and asserts the server really launched, so a validation failure can never pass a test vacuously.
    private async Task StartAsync(Harness h, bool isManual = true)
    {
        await h.Vm.StartServerAsync(isManual);
        Assert.True(h.Vm.Server!.Status == ServerStatus.Starting,
            "Server did not start. Messages: " + string.Join(" | ", h.Messages.Select(m => m.Body)));
    }

    private static PlayerInfo Steam(string id) => new() { Platform = "Steam", PlatformRaw = "Steam", PlayerId = id };

    private async Task StartRunningAsync(Harness h, bool isManual = true)
    {
        await StartAsync(h, isManual);
        var server = h.Vm.Server!;
        server.Logger!.Information("Game server connected"); // parser promotes Starting → Running
        Assert.Equal(ServerStatus.Running, server.Status);
    }

    // Restart re-enters Core Start after the old process exits; drive the exit and wait for the relaunch.
    private void Restart(Harness h)
    {
        var server = h.Vm.Server!;
        h.Vm.RestartCommand.Execute(null);
        _processes.Last().SimulateExit();
        var sw = Stopwatch.StartNew();
        while (server.Status != ServerStatus.Starting && sw.Elapsed < TimeSpan.FromSeconds(10)) Thread.Sleep(20);
        Assert.Equal(ServerStatus.Starting, server.Status);
        server.Logger!.Information("Game server connected");
        // The relaunch ran on a background thread, so the view-model's reactions (saving adopted roles) were posted
        // to the UI dispatcher; run them as the UI loop would.
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
    }

    private string ListPath(string file) => Path.Combine(_saveDir, file);

    private void Seed(string file, params string[] lines)
        => File.WriteAllText(ListPath(file), "// header\n" + string.Join('\n', lines) + "\n");

    // Stands in for the game's own in-game ban/permit (SyncedList.Save) or a hand edit made during play.
    private void AppendDuringPlay(string file, string entry)
        => File.AppendAllText(ListPath(file), entry + "\n");

    private string[] Lines(string file) => File.Exists(ListPath(file)) ? File.ReadAllLines(ListPath(file)) : Array.Empty<string>();

    private bool ListContains(string file, string id) => Lines(file).Select(l => l.Trim()).Any(l => l == id || l == $"Steam_{id}");

    // ===== The invariant =====

    // Identity of an entry: a resolvable token becomes "Platform:Id" (so bare and prefixed Steam forms match);
    // anything VSG can't resolve is compared verbatim.
    private static string Identity(string token)
        => PlayerListToken.TryResolve(token, out var platform, out _, out var id) ? $"{platform}:{id}" : token.Trim();

    private static IEnumerable<string> EntryLines(string path)
        => File.Exists(path)
            ? File.ReadAllLines(path).Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith("//"))
            : Enumerable.Empty<string>();

    /// <summary>Every entry of every list file, as (file, identity) pairs.</summary>
    private HashSet<(string File, string Id)> SnapshotEntries()
        => new[] { Admin, Banned, Permitted }
            .SelectMany(f => EntryLines(ListPath(f)).Select(e => (f, Identity(e))))
            .ToHashSet();

    /// <summary>
    /// Asserts nothing in <paramref name="before"/> was lost: each entry is still in its list file or in one of that
    /// file's backups (<c>name.bak*.txt</c>), unless listed in <paramref name="intended"/>.
    /// </summary>
    private void AssertNoEntryLost(HashSet<(string File, string Id)> before, params (string File, string Id)[] intended)
    {
        var lost = new List<string>();
        foreach (var (file, id) in before)
        {
            if (intended.Contains((file, id))) continue;

            var stem = Path.GetFileNameWithoutExtension(file);
            var survivors = new[] { ListPath(file) }
                .Concat(Directory.GetFiles(_saveDir, stem + ".bak*.txt"))
                .SelectMany(EntryLines)
                .Select(Identity);
            if (!survivors.Contains(id)) lost.Add($"{file}: {id}");
        }
        Assert.True(lost.Count == 0, "List entries were erased with no backup:\n  " + string.Join("\n  ", lost));
    }

    private static (string, string) E(string file, string id) => (file, $"Steam:{id}");

    // ===== Start =====

    [Fact]
    public async Task Start_upgrade_with_an_unrecognized_line_keeps_every_entry()
    {
        Seed(Admin, $"Steam_{SteamA}", SteamB, Unrecognized);
        Seed(Banned, $"Steam_{SteamC}");
        var h = Build();
        var before = SnapshotEntries();

        await StartAsync(h);

        AssertNoEntryLost(before);
        Assert.Contains(Unrecognized, Lines(Admin)); // carried through, not just backed up
        Assert.True(ListContains(Banned, SteamC));    // an unrelated file is untouched in effect
    }

    [Fact]
    public async Task Start_with_roles_and_an_unrecognized_line_keeps_every_entry()
    {
        Seed(Admin, $"Steam_{SteamA}", Unrecognized);
        var h = Build(ProfileWith((SteamA, PlayerRole.Admin)));
        var before = SnapshotEntries();

        await StartAsync(h);

        AssertNoEntryLost(before);
        Assert.Contains(Unrecognized, Lines(Admin));
    }

    [Fact]
    public async Task Start_keeps_comment_lines()
    {
        Seed(Admin, "// Bob's alt account", $"Steam_{SteamA}");
        var h = Build(ProfileWith((SteamA, PlayerRole.Admin)));

        await StartAsync(h);

        Assert.Contains("// Bob's alt account", Lines(Admin));
    }

    [Fact]
    public async Task Start_upgrade_with_a_player_in_two_lists_keeps_both_entries()
    {
        Seed(Admin, SteamA);
        Seed(Banned, SteamA);
        var h = Build();
        var before = SnapshotEntries();

        await StartAsync(h);

        AssertNoEntryLost(before);
    }

    [Fact]
    public async Task Start_conflict_use_server_profile_backs_up_the_overwritten_entry()
    {
        Seed(Admin, SteamA);
        var h = Build(ProfileWith((SteamA, PlayerRole.Banned)));
        var before = SnapshotEntries();

        await StartAsync(h);

        Assert.Single(h.ConflictBodies);
        AssertNoEntryLost(before);
        Assert.True(ListContains(Banned, SteamA)); // the profile won
    }

    [Fact]
    public async Task Start_conflict_backup_failure_aborts_and_leaves_the_files_alone()
    {
        Seed(Admin, SteamA);
        // A directory squatting on every backup name the writer could pick makes the backup impossible.
        Directory.CreateDirectory(ListPath("adminlist.bak.txt"));
        for (var i = 2; i <= 5; i++) Directory.CreateDirectory(ListPath($"adminlist.bak.{i}.txt"));
        var h = Build(ProfileWith((SteamA, PlayerRole.Banned)));
        var adminBefore = File.ReadAllText(ListPath(Admin));

        await h.Vm.StartServerAsync(isManual: true);

        Assert.Equal(ServerStatus.Stopped, h.Vm.Server!.Status);    // not started
        Assert.Equal(adminBefore, File.ReadAllText(ListPath(Admin))); // not overwritten
    }

    [Fact]
    public async Task Start_conflict_use_roles_from_file_survives_a_later_live_edit()
    {
        Seed(Admin, SteamA);
        var h = Build(ProfileWith((SteamA, PlayerRole.Banned)));
        h.ConflictResult = RoleConflictChoice.UseRolesFromFile;
        await StartRunningAsync(h);

        h.Vm.Form.SetRole(Steam(SteamC), PlayerRole.Admin); // any live edit regenerates the files

        Assert.True(ListContains(Admin, SteamA)); // the files' role was kept, not reverted to the profile
        Assert.Equal(PlayerRole.Admin, h.Vm.Form.GetOverride($"Steam:{SteamA}"));
    }

    [Fact]
    public async Task Start_after_an_offline_role_change_is_not_a_conflict()
    {
        var h = Build(ProfileWith((SteamA, PlayerRole.Admin)));
        await StartRunningAsync(h);
        _processes.Last().SimulateExit();
        Assert.Equal(ServerStatus.Stopped, h.Vm.Server!.Status);

        h.Vm.Form.SetRole(Steam(SteamA), PlayerRole.Banned); // VSG's own change while stopped
        await StartAsync(h);

        Assert.Empty(h.ConflictBodies);              // VSG's own previous write is not a conflict
        Assert.False(ListContains(Admin, SteamA));
        Assert.True(ListContains(Banned, SteamA));
        Assert.Empty(Directory.GetFiles(_saveDir, "*.bak*.txt")); // nothing external was dropped
    }

    // ===== Live updates during play (the game's in-game commands, or hand edits) =====

    [Fact]
    public async Task In_game_ban_survives_a_restart_and_is_adopted()
    {
        var h = Build(ProfileWith((SteamA, PlayerRole.Admin)));
        await StartRunningAsync(h);
        AppendDuringPlay(Banned, $"Steam_{SteamB}");
        var before = SnapshotEntries();

        Restart(h);

        AssertNoEntryLost(before);
        Assert.True(ListContains(Banned, SteamB));
        Assert.Equal(PlayerRole.Banned, h.Vm.Form.GetOverride($"Steam:{SteamB}"));
        Assert.Equal(PlayerRole.Banned, _serverPrefs.LoadPreferences("Safety")!.PlayerRoles[$"Steam:{SteamB}"].Role);
    }

    [Fact]
    public async Task In_game_ban_survives_a_live_role_edit_and_is_adopted()
    {
        var h = Build(ProfileWith((SteamA, PlayerRole.Admin)));
        await StartRunningAsync(h);
        AppendDuringPlay(Banned, $"Steam_{SteamB}");
        var before = SnapshotEntries();

        h.Vm.Form.SetRole(Steam(SteamC), PlayerRole.Admin);

        AssertNoEntryLost(before);
        Assert.True(ListContains(Banned, SteamB));
        Assert.Equal(PlayerRole.Banned, h.Vm.Form.GetOverride($"Steam:{SteamB}"));
    }

    [Fact]
    public async Task In_game_ban_survives_saving_player_defaults()
    {
        var h = Build(ProfileWith((SteamA, PlayerRole.Admin)));
        await StartRunningAsync(h);
        AppendDuringPlay(Banned, $"Steam_{SteamB}");
        var before = SnapshotEntries();

        SaveDefaults((SteamC, PlayerRole.Admin)); // Manage Players save → live apply

        AssertNoEntryLost(before);
        Assert.True(ListContains(Banned, SteamB));
    }

    [Fact]
    public async Task In_game_ban_of_an_admin_wins_over_the_profile()
    {
        var h = Build(ProfileWith((SteamA, PlayerRole.Admin)));
        await StartRunningAsync(h);
        AppendDuringPlay(Banned, $"Steam_{SteamA}");

        Restart(h);

        Assert.Equal(PlayerRole.Banned, h.Vm.Form.GetOverride($"Steam:{SteamA}"));
        Assert.True(ListContains(Banned, SteamA));
        Assert.False(ListContains(Admin, SteamA));
    }

    [Fact]
    public async Task In_game_unban_removes_the_server_ban()
    {
        var h = Build(ProfileWith((SteamA, PlayerRole.Banned)));
        await StartRunningAsync(h);
        Assert.True(ListContains(Banned, SteamA));
        File.WriteAllText(ListPath(Banned), "// List banned players ID  ONE per line\n"); // in-game unban

        Restart(h);

        Assert.Null(h.Vm.Form.GetOverride($"Steam:{SteamA}"));
        Assert.False(ListContains(Banned, SteamA));
    }

    [Fact]
    public async Task In_game_unban_of_a_global_ban_keeps_the_global_ban()
    {
        SaveDefaults((SteamA, PlayerRole.Banned));
        var h = Build();
        await StartRunningAsync(h);
        Assert.True(ListContains(Banned, SteamA));
        File.WriteAllText(ListPath(Banned), "// List banned players ID  ONE per line\n"); // in-game unban

        Restart(h);

        Assert.Null(h.Vm.Form.GetOverride($"Steam:{SteamA}"));
        Assert.True(ListContains(Banned, SteamA)); // the global ban is reapplied
    }

    [Fact]
    public async Task Removing_a_role_in_VSG_during_play_is_not_undone()
    {
        var h = Build(ProfileWith((SteamA, PlayerRole.Banned)));
        await StartRunningAsync(h);

        h.Vm.Form.SetRole(Steam(SteamA), null); // the user unbans in VSG

        Assert.False(ListContains(Banned, SteamA));
        Assert.Null(h.Vm.Form.GetOverride($"Steam:{SteamA}"));
        Assert.Empty(Directory.GetFiles(_saveDir, "*.bak*.txt")); // VSG's own entry needs no backup
    }

    // ===== Import =====

    [Fact]
    public async Task Import_with_an_unrecognized_line_imports_the_rest()
    {
        var h = Build(ProfileWith((SteamC, PlayerRole.Permitted)));
        Seed(Admin, SteamA, Unrecognized);

        await h.Vm.RunImportAsync(interactive: true);

        Assert.Equal(PlayerRole.Admin, h.Vm.Form.GetOverride($"Steam:{SteamA}"));
        Assert.Contains(Unrecognized, Lines(Admin)); // import never writes
    }
}
