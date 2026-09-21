using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using ValheimServerGUI.Core.Tests.Fakes;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools.Data;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Game;

/// <summary>
/// The per-profile server registry: idempotent creation, the snapshot of All, the server-owned log buffer
/// (append + cap), Remove (dispose + drop), and StopAllAndDispose (waits for Stopped, then clears).
/// </summary>
public sealed class ServerManagerTests : IDisposable
{
    private readonly string _dir;
    private readonly string _exe;
    private readonly string _saveDir;
    private readonly LinuxValheimPathResolver _resolver;

    // A single mock process provider is shared by the servers the factory builds — fine here, since these
    // tests only ever start one server at a time when they exercise the process path.
    private MockProcessProvider _lastProcessProvider = new();

    public ServerManagerTests()
    {
        _dir = Path.Join(Path.GetTempPath(), "vsg-mgr-" + Guid.NewGuid().ToString("n"));
        _saveDir = Path.Join(_dir, "save");
        Directory.CreateDirectory(_saveDir);
        _exe = Path.Join(_dir, "valheim_server.x86_64");
        File.WriteAllText(_exe, "");
        _resolver = new LinuxValheimPathResolver("/tmp/vsg-test-home", xdgDataHome: null);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private ServerManager NewManager()
    {
        ILogger serilog = new LoggerConfiguration().CreateLogger();

        ValheimServer Factory()
        {
            _lastProcessProvider = new MockProcessProvider();
            var context = new DataFileRepositoryContext(new MockDataFileProvider(), serilog);
            var repo = new PlayerDataRepository(context, new FakeRuneberryApiClient(), _resolver);
            return new ValheimServer(_lastProcessProvider, repo, new FakeApplicationLogger(), _resolver, new PlayerAccessListService());
        }

        return new ServerManager(Factory, serilog);
    }

    private ValheimServerOptions Options() => new()
    {
        Name = "My Server",
        WorldName = "MyWorld",
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

    private static void WaitFor(Func<bool> condition, int timeoutMs = 2000)
    {
        var sw = Stopwatch.StartNew();
        while (!condition() && sw.ElapsedMilliseconds < timeoutMs) Thread.Sleep(10);
    }

    [Fact]
    public void GetOrCreate_is_idempotent_and_case_insensitive()
    {
        var mgr = NewManager();

        var a1 = mgr.GetOrCreate("Alpha");
        var a2 = mgr.GetOrCreate("Alpha");
        var aUpper = mgr.GetOrCreate("ALPHA");
        var b = mgr.GetOrCreate("Beta");

        Assert.Same(a1, a2);
        Assert.Same(a1, aUpper);       // keyed like ServerPreferences (case-insensitive)
        Assert.NotSame(a1, b);
    }

    [Fact]
    public void All_reflects_created_servers()
    {
        var mgr = NewManager();
        var a = mgr.GetOrCreate("A");
        var b = mgr.GetOrCreate("B");

        Assert.Equal(2, mgr.All.Count);
        Assert.Contains(a, mgr.All);
        Assert.Contains(b, mgr.All);
    }

    [Fact]
    public void TryGet_returns_the_created_server_or_false()
    {
        var mgr = NewManager();
        var a = mgr.GetOrCreate("A");

        Assert.True(mgr.TryGet("a", out var got));
        Assert.Same(a, got);
        Assert.False(mgr.TryGet("Nope", out _));
    }

    [Fact]
    public void GetLogAppender_appends_to_the_owned_buffer_and_caps_it()
    {
        var mgr = NewManager();
        var buffer = mgr.GetServerLog("A");
        var append = mgr.GetLogAppender("A");

        for (var i = 0; i < 5100; i++) append($"line {i}");

        Assert.Equal(5000, buffer.Count);
        Assert.DoesNotContain("line 0", buffer);     // oldest dropped off the top
        Assert.Contains("line 5099", buffer);         // newest kept
    }

    [Fact]
    public void Remove_disposes_and_drops_the_server()
    {
        var mgr = NewManager();
        mgr.GetOrCreate("A");
        mgr.GetOrCreate("B");

        mgr.Remove("A");

        Assert.False(mgr.TryGet("A", out _));
        Assert.Single(mgr.All);
        Assert.True(mgr.TryGet("B", out _));
    }

    [Fact]
    public void StopAllAndDispose_clears_the_registry()
    {
        var mgr = NewManager();
        mgr.GetOrCreate("A"); // never started → already Stopped
        mgr.GetOrCreate("B");

        mgr.StopAllAndDispose();

        Assert.Empty(mgr.All);
    }

    [Fact]
    public void StopAllAndDispose_stops_a_running_server_and_waits_for_stopped()
    {
        var mgr = NewManager();
        var server = mgr.GetOrCreate("A");
        server.Start(Options());
        Assert.Equal(ServerStatus.Starting, server.Status);
        var process = _lastProcessProvider;

        // StopAllAndDispose blocks until the server reports Stopped; simulate the process exiting from this
        // thread once the graceful kill has been dispatched, so the wait completes deterministically.
        var task = Task.Run(mgr.StopAllAndDispose);
        WaitFor(() => process.SafelyKilledKeys.Count > 0);
        process.SimulateExit(); // → Stopped

        Assert.True(task.Wait(TimeSpan.FromSeconds(5)));
        Assert.Equal(ServerStatus.Stopped, server.Status);
        Assert.Empty(mgr.All);
    }
}
