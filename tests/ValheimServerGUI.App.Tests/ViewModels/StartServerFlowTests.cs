using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ValheimServerGUI.App.Services;
using ValheimServerGUI.App.Tests.Fakes;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.Game;
using ValheimServerGUI.Localization;
using ValheimServerGUI.Tools;
using Xunit;

namespace ValheimServerGUI.App.Tests.ViewModels;

// StartServer flow: cloud import → validate → port check → new-world checks → start → save-on-start.
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
        public required FakeIpAddressProvider Ip { get; init; }
        public required FakeWorldPreferencesProvider WorldPrefs { get; init; }
        public List<string> WorldSettingsPrompts { get; } = new();
        public bool WorldSettingsAnswer { get; set; } = true;
        public List<string> Errors { get; } = new();
        public List<IValheimServerOptions> Started { get; } = new();
    }

    private Harness Build(CloudImportChoice cloudChoice = CloudImportChoice.Copy, params string[] cloudWorlds)
    {
        var cloud = new FakeSteamCloudWorldProvider(cloudWorlds);
        var serverPrefs = new FakeServerPreferencesProvider();
        var ip = new FakeIpAddressProvider();
        var worldPrefs = new FakeWorldPreferencesProvider();
        var shell = new ShellLauncher(new Services.RecordingSystemShell(), TestLog.Silent);

        var manager = new ServerManager(
            () => Core.GetRequiredService<ValheimServer>(),
            Core.GetRequiredService<Serilog.ILogger>());
        var vm = new MainWindowViewModel(
            manager,
            // The directories come from the app-wide paths (the profile has no overrides).
            new FakeUserPreferencesProvider(new UserPreferences { ServerExePath = _exe, SaveDataFolderPath = _saveDir }),
            serverPrefs,
            worldPrefs,
            cloud,
            ip,
            Core.GetRequiredService<IPlayerDataRepository>(),
            Core.GetRequiredService<ValheimServerGUI.Tools.Logging.IApplicationLogger>(),
            new FakeSoftwareUpdateProvider(),
            shell,
            Core.GetRequiredService<IValheimPathResolver>(),
            Core.GetRequiredService<IPlayerListImportService>());

        vm.LoadProfile(new ServerPreferences { ProfileName = "Test" });

        var harness = new Harness { Vm = vm, Cloud = cloud, ServerPrefs = serverPrefs, Ip = ip, WorldPrefs = worldPrefs };
        vm.ErrorReported = harness.Errors.Add;
        vm.CloudImportPrompt = _ => Task.FromResult(cloudChoice);
        vm.StartAction = harness.Started.Add; // don't actually launch
        vm.WorldSettingsChangePrompt = body =>
        {
            harness.WorldSettingsPrompts.Add(body);
            return Task.FromResult(harness.WorldSettingsAnswer);
        };

        // A valid baseline configuration; individual tests tweak the form.
        vm.Form.Name = "MyServer";
        vm.Form.Password = "hunter2";
        vm.Form.Port = 2456;
        return harness;
    }

    private void CreateLocalWorld(string name)
        => File.WriteAllText(Path.Combine(_saveDir, "worlds", name + ".fwl"), string.Empty);

    /// <summary>Writes a committed 1.0+ world save storing <paramref name="storedKeys"/>, laid out like the game's.</summary>
    private void CreateLocalWorldSave(string name, IEnumerable<string> storedKeys)
    {
        var folder = Path.Combine(_saveDir, "worlds_local", name);
        Directory.CreateDirectory(folder);

        using var payload = new MemoryStream();
        using (var writer = new BinaryWriter(payload))
        {
            writer.Write(WorldMetadataReader.PlayerHistoryVersion);
            writer.Write(name);
            writer.Write("seed");
            writer.Write(1);
            writer.Write(2L);
            writer.Write(2); // worldGenVersion
            writer.Write(true); // needsDB
            var keys = storedKeys.ToList();
            writer.Write(keys.Count);
            foreach (var key in keys) writer.Write(key);
            writer.Write(0); // player history
        }

        var bytes = payload.ToArray(); // the writer closed the stream; ToArray still works
        using (var file = new BinaryWriter(File.Create(Path.Combine(folder, "_main.1.fwl2"))))
        {
            file.Write(bytes.Length);
            file.Write(bytes);
        }
        File.WriteAllBytes(Path.Combine(folder, "_main.1.ok"), Array.Empty<byte>());
    }

    private Harness BuildExistingWorld(string name, WorldGenSettings stored, WorldGenSettings preferred)
    {
        CreateLocalWorldSave(name, stored.ToGameKeys().Append("preset " + name.ToLowerInvariant()));
        var h = Build();
        h.WorldPrefs.SavePreferences(new WorldPreferences
        {
            WorldName = name,
            Modifiers = new(preferred.Modifiers),
            Keys = preferred.Keys.ToHashSet(),
        });
        h.Vm.RefreshWorldsCommand.Execute(null);
        h.Vm.Form.UseNewWorld = false;
        h.Vm.Form.ExistingWorld = name;
        return h;
    }

    private static readonly WorldGenSettings CombatHard =
        new(new Dictionary<string, string> { [WorldGenModifiers.Combat] = WorldGenModifiers.Values.CombatHard });

    // ----- World settings: the launch makes the world match World Preferences; changing what it stores asks first -----

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Existing_world_with_a_setting_the_launch_changes_asks_first(bool answer)
    {
        var h = BuildExistingWorld("Hardworld", stored: CombatHard, preferred: new WorldGenSettings());
        h.WorldSettingsAnswer = answer;

        await h.Vm.StartServerAsync(isManual: true);

        var prompt = Assert.Single(h.WorldSettingsPrompts);
        Assert.Contains(string.Format(Strings.WorldGen_Change_Modifier,
            Strings.WorldGen_Combat_Name, Strings.WorldGen_Combat_Hard, Strings.WorldGen_Value_Normal), prompt);
        Assert.Contains("Hardworld", prompt);
        Assert.Equal(answer ? 1 : 0, h.Started.Count);
    }

    [Fact]
    public async Task Existing_world_that_already_matches_starts_without_asking()
    {
        var h = BuildExistingWorld("Hardworld", stored: CombatHard, preferred: CombatHard);

        await h.Vm.StartServerAsync(isManual: true);

        Assert.Empty(h.WorldSettingsPrompts);
        Assert.Single(h.Started);
    }

    [Fact]
    public async Task Existing_world_that_only_gains_settings_starts_without_asking()
    {
        var hardcore = WorldGenPresets.Find(WorldGenPresets.Hardcore)!.Settings;
        var h = BuildExistingWorld("Plainworld", stored: new WorldGenSettings(), preferred: hardcore);

        await h.Vm.StartServerAsync(isManual: true);

        Assert.Empty(h.WorldSettingsPrompts);
        Assert.Single(h.Started);
    }

    // A key VSG does not own (console, mod, newer game) is carried over by the launch, so there is nothing to confirm.
    [Fact]
    public async Task Existing_world_with_only_keys_vsg_does_not_own_starts_without_asking()
    {
        CreateLocalWorldSave("Moddedworld", new[] { "nocraftcost", "somefuturekey 50" });
        var h = Build();
        h.Vm.RefreshWorldsCommand.Execute(null);
        h.Vm.Form.UseNewWorld = false;
        h.Vm.Form.ExistingWorld = "Moddedworld";

        await h.Vm.StartServerAsync(isManual: true);

        Assert.Empty(h.WorldSettingsPrompts);
        Assert.Single(h.Started);
    }

    // Like the list files, auto-start has nobody to ask and applies World Preferences.
    [Fact]
    public async Task Auto_start_applies_world_settings_without_asking()
    {
        var h = BuildExistingWorld("Hardworld", stored: CombatHard, preferred: new WorldGenSettings());

        await h.Vm.StartServerAsync(isManual: false);

        Assert.Empty(h.WorldSettingsPrompts);
        Assert.Single(h.Started);
    }

    [Fact]
    public async Task Unreadable_world_save_starts_without_asking()
    {
        var h = BuildExistingWorld("Hardworld", stored: CombatHard, preferred: new WorldGenSettings());
        File.WriteAllBytes(Path.Combine(_saveDir, "worlds_local", "Hardworld", "_main.1.fwl2"), new byte[] { 1, 2, 3 });

        await h.Vm.StartServerAsync(isManual: true);

        Assert.Empty(h.WorldSettingsPrompts);
        Assert.Single(h.Started);
    }

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

        Assert.Contains(Strings.Validation_WorldNameLength, h.Errors);
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

        Assert.Contains(string.Format(Strings.Validation_WorldNameTaken, "Existingworld"), h.Errors);
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

        Assert.Contains(string.Format(Strings.Validation_WorldNotFound, "Ghostworld"), h.Errors);
        Assert.Empty(h.Started);
    }

    // Valheim needs the server port and the one above it; either being taken blocks the start.
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task Port_in_use_surfaces_error(int occupiedOffset)
    {
        var h = Build();
        CreateLocalWorld("Someworld");
        h.Vm.Form.UseNewWorld = false;
        h.Vm.Form.ExistingWorld = "Someworld";
        var port = h.Vm.Form.Port;
        h.Ip.OccupiedPorts.Add(port + occupiedOffset);

        await h.Vm.StartServerAsync(isManual: true);

        Assert.Contains(string.Format(Strings.Validation_PortInUse, port, port + 1), h.Errors);
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
