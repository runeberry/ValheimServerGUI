using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ValheimServerGUI.App.Services;
using ValheimServerGUI.App.Tests.Fakes;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;
using Xunit;

namespace ValheimServerGUI.App.Tests.ViewModels;

// §10.3 StartServer flow: cloud import → validate → port check → new-world checks → start → save-on-start.
public sealed class StartServerFlowTests : IDisposable
{
    private static readonly IServiceProvider Core =
        new ServiceCollection().AddValheimCore().BuildServiceProvider();

    private readonly string _dir;
    private readonly string _saveDir;
    private readonly string _exe;

    public StartServerFlowTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "vsg-flow-" + Guid.NewGuid().ToString("N"));
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
        public required FakeSteamCloudWorldProvider Cloud { get; init; }
        public required FakeServerPreferencesProvider ServerPrefs { get; init; }
        public List<string> Errors { get; } = new();
        public List<IValheimServerOptions> Started { get; } = new();
    }

    private Harness Build(CloudImportChoice cloudChoice = CloudImportChoice.Copy, params string[] cloudWorlds)
    {
        var cloud = new FakeSteamCloudWorldProvider(cloudWorlds);
        var serverPrefs = new FakeServerPreferencesProvider();
        var shell = new ShellLauncher(new Services.RecordingSystemShell(), TestLog.Silent);

        var manager = new ServerManager(
            () => Core.GetRequiredService<ValheimServer>(),
            Core.GetRequiredService<Serilog.ILogger>());
        var vm = new MainWindowViewModel(
            manager,
            new FakeUserPreferencesProvider(),
            serverPrefs,
            Core.GetRequiredService<IWorldPreferencesProvider>(),
            cloud,
            Core.GetRequiredService<IIpAddressProvider>(),
            Core.GetRequiredService<IPlayerDataRepository>(),
            Core.GetRequiredService<ValheimServerGUI.Tools.Logging.IApplicationLogger>(),
            new FakeSoftwareUpdateProvider(),
            shell,
            Core.GetRequiredService<IValheimPathResolver>(),
            Core.GetRequiredService<IPlayerListImportService>(),
            Core.GetRequiredService<IRuneberryApiClient>());

        vm.LoadProfile(new ServerPreferences { ProfileName = "Test" });

        var harness = new Harness { Vm = vm, Cloud = cloud, ServerPrefs = serverPrefs };
        vm.ErrorReported = harness.Errors.Add;
        vm.CloudImportPrompt = _ => Task.FromResult(cloudChoice);
        vm.StartAction = harness.Started.Add; // don't actually launch

        // A valid baseline configuration; individual tests tweak the form.
        vm.Form.Name = "MyServer";
        vm.Form.Password = "hunter2";
        vm.Form.Port = FreeUdpPort();
        vm.Form.ServerExePath = _exe;
        vm.Form.SaveDataFolderPath = _saveDir;
        return harness;
    }

    private static int FreeUdpPort()
    {
        using var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        s.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)s.LocalEndPoint!).Port;
    }

    private void CreateLocalWorld(string name)
        => File.WriteAllText(Path.Combine(_saveDir, "worlds", name + ".fwl"), string.Empty);

    [Fact]
    public async Task New_world_valid_starts_and_saves_profile()
    {
        var h = Build();
        h.Vm.Form.UseNewWorld = true;
        h.Vm.Form.NewWorldName = "Freshworld";

        await h.Vm.StartServerAsync(isManual: true);

        Assert.Empty(h.Errors);
        var opts = Assert.Single(h.Started);
        Assert.Equal("Freshworld", opts.WorldName);
        Assert.NotNull(h.ServerPrefs.LoadPreferences("Test")); // save-on-start
    }

    [Fact]
    public async Task Server_name_equal_to_world_name_surfaces_validation_error()
    {
        var h = Build();
        h.Vm.Form.UseNewWorld = true;
        h.Vm.Form.Name = "Sameworld";
        h.Vm.Form.NewWorldName = "Sameworld";

        await h.Vm.StartServerAsync(isManual: true);

        Assert.NotEmpty(h.Errors);
        Assert.Empty(h.Started);
    }

    [Fact]
    public async Task New_world_name_too_short_surfaces_error()
    {
        var h = Build();
        h.Vm.Form.UseNewWorld = true;
        h.Vm.Form.NewWorldName = "abc";

        await h.Vm.StartServerAsync(isManual: true);

        Assert.Contains(h.Errors, e => e.Contains("5-20"));
        Assert.Empty(h.Started);
    }

    [Fact]
    public async Task New_world_name_taken_surfaces_error_and_switches_to_existing()
    {
        CreateLocalWorld("Existingworld");
        var h = Build();
        h.Vm.Form.UseNewWorld = true;
        h.Vm.Form.NewWorldName = "Existingworld";

        await h.Vm.StartServerAsync(isManual: true);

        Assert.Contains(h.Errors, e => e.Contains("already exists"));
        Assert.False(h.Vm.Form.UseNewWorld); // switched back to Existing
        Assert.Equal("Existingworld", h.Vm.Form.ExistingWorld);
        Assert.Empty(h.Started);
    }

    [Fact]
    public async Task Existing_world_that_does_not_exist_surfaces_error()
    {
        var h = Build();
        h.Vm.Form.UseNewWorld = false;
        h.Vm.Form.ExistingWorld = "Ghostworld";

        await h.Vm.StartServerAsync(isManual: true);

        Assert.Contains(h.Errors, e => e.Contains("No world exists"));
        Assert.Empty(h.Started);
    }

    [Fact]
    public async Task Port_in_use_surfaces_error()
    {
        var h = Build();
        CreateLocalWorld("Someworld");
        h.Vm.Form.UseNewWorld = false;
        h.Vm.Form.ExistingWorld = "Someworld";

        var port = FreeUdpPort();
        using var occupied = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        occupied.Bind(new IPEndPoint(IPAddress.Loopback, port));
        h.Vm.Form.Port = port;

        await h.Vm.StartServerAsync(isManual: true);

        Assert.Contains(h.Errors, e => e.Contains("already in use"));
        Assert.Empty(h.Started);
    }

    [Fact]
    public async Task Cloud_world_cancel_does_not_import_or_start()
    {
        var h = Build(CloudImportChoice.Cancel, cloudWorlds: "Cloudworld");
        h.Vm.RefreshWorldsCommand.Execute(null);
        h.Vm.Form.UseNewWorld = false;
        h.Vm.Form.ExistingWorld = "Cloudworld" + AppConstants.CloudWorldSuffix;

        await h.Vm.StartServerAsync(isManual: true);

        Assert.Empty(h.Cloud.Imports);
        Assert.Empty(h.Started);
    }

    [Theory]
    [InlineData(CloudImportChoice.Copy, false)]
    [InlineData(CloudImportChoice.Move, true)]
    public async Task Cloud_world_import_then_start(CloudImportChoice choice, bool expectMove)
    {
        var h = Build(choice, cloudWorlds: "Cloudworld");
        h.Vm.RefreshWorldsCommand.Execute(null);
        h.Vm.Form.UseNewWorld = false;
        h.Vm.Form.ExistingWorld = "Cloudworld" + AppConstants.CloudWorldSuffix;

        await h.Vm.StartServerAsync(isManual: true);

        var import = Assert.Single(h.Cloud.Imports);
        Assert.Equal("Cloudworld", import.World);
        Assert.Equal(expectMove, import.Move);
        var opts = Assert.Single(h.Started); // imported world is now local → start proceeds
        Assert.Equal("Cloudworld", opts.WorldName);
    }

    [Fact]
    public async Task New_world_reselected_as_existing_once_running()
    {
        var h = Build();
        h.Vm.Form.UseNewWorld = true;
        h.Vm.Form.NewWorldName = "Bornworld";

        await h.Vm.StartServerAsync(isManual: true);
        Assert.Single(h.Started);

        // Simulate the server creating the world and reaching Running.
        CreateLocalWorld("Bornworld");
        h.Vm.ServerStatus = ServerStatus.Running;

        Assert.False(h.Vm.Form.UseNewWorld);
        Assert.Equal("Bornworld", h.Vm.Form.ExistingWorld);
    }
}
