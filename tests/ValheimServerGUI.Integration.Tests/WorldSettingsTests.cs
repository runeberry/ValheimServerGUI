using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ValheimServerGUI.Game;
using Xunit;

namespace ValheimServerGUI.Integration.Tests
{
    /// <summary>
    /// The world settings the real server stores after VSG launches it. The game only adds or replaces stored settings
    /// on <c>-modifier</c>, so VSG launches with <c>-resetmodifiers</c> to make the world match World Preferences; the
    /// first test proves a stored value set back to Normal does not survive. The preset rows compare the binary's own
    /// <c>-preset</c> with VSG's preset table, so a game update that changes a preset fails here.
    /// </summary>
    public sealed class WorldSettingsTests : IClassFixture<WorldSettingsTests.PresetWorld>
    {
        // A first boot generates the world; later boots of the same world are quicker.
        private static readonly TimeSpan BootTimeout = TimeSpan.FromSeconds(150);
        private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(150);
        private static readonly TimeSpan SaveCommitTimeout = TimeSpan.FromSeconds(30);

        private readonly PresetWorld _presetWorld;

        public WorldSettingsTests(PresetWorld presetWorld) => _presetWorld = presetWorld;

        /// <summary>One generated world the preset rows reuse, so each row boots an existing world.</summary>
        public sealed class PresetWorld : IAsyncLifetime
        {
            public string WorldName { get; } = "ITPreset" + Guid.NewGuid().ToString("n")[..6];

            public async ValueTask InitializeAsync() => await BootAndStopAsync(WorldName);

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }

        private sealed record Run(IReadOnlyList<string> StoredKeys, IReadOnlyList<string> Log);

        /// <summary>
        /// Boots the world to Running, stops it gracefully, and reads what the stop's save stores. Without a new save
        /// the newest one on disk is the previous launch's, so a missing save fails here rather than passing off old
        /// settings as this launch's. Each launch's server log is written as an artifact.
        /// </summary>
        private static async Task<Run> BootAndStopAsync(string worldName, Action<ValheimServerOptions>? configure = null)
        {
            using var harness = new LiveServerHarness(IntegrationConfig.Current.SaveDir, worldName);
            // A fresh DirectoryInfo per look: one created before the world exists caches Exists = false.
            string? LatestSave() => WorldMetadataReader.FindLatestCompleteSave(
                new DirectoryInfo(harness.SaveDir).GetLocalWorldFolder(worldName))?.Name;
            var saveBefore = LatestSave();

            try
            {
                if (!await harness.BootToRunningAsync(BootTimeout, configure))
                    throw new InvalidOperationException($"World {worldName} never reached Running.{Tail(harness.Log)}");
                if (!await harness.StopGracefullyAsync(StopTimeout))
                    throw new InvalidOperationException($"World {worldName} did not stop gracefully.{Tail(harness.Log)}");

                // The save thread can still be writing when the process is reported stopped; the game commits a save
                // by writing its .ok marker last, so wait for a newer committed save.
                var saveAfter = LatestSave();
                var deadline = DateTime.UtcNow + SaveCommitTimeout;
                while ((saveAfter is null || saveAfter == saveBefore) && DateTime.UtcNow < deadline)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(250));
                    saveAfter = LatestSave();
                }
                if (saveAfter is null || saveAfter == saveBefore)
                    throw new InvalidOperationException(
                        $"World {worldName} wrote no new save on stop (newest is still {saveAfter ?? "none"}).{Tail(harness.Log)}");

                return new Run(harness.ReadStoredWorldKeys(), harness.Log.ToArray());
            }
            finally
            {
                File.WriteAllLines(Path.Join(IntegrationConfig.Current.ArtifactDir,
                    $"live-server-{worldName}-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.log"), harness.Log.ToArray());
            }
        }

        private static string Tail(IEnumerable<string> log)
            => Environment.NewLine + string.Join(Environment.NewLine, log.TakeLast(30));

        private static IEnumerable<string> WithoutPresetMarker(IEnumerable<string> keys)
            => keys.Where(k => !k.Split(' ')[0].Equals("preset", StringComparison.OrdinalIgnoreCase))
                .Select(k => k.ToLowerInvariant())
                .OrderBy(k => k);

        [Fact]
        public async Task A_stored_modifier_set_back_to_normal_does_not_survive_a_launch()
        {
            var worldName = "ITReset" + Guid.NewGuid().ToString("n")[..6];
            var normal = new WorldGenSettings();

            // A world storing Combat Hard, as one set up in the game would.
            var hard = await BootAndStopAsync(worldName, o => o.WorldModifiers = new()
            {
                [WorldGenModifiers.Combat] = WorldGenModifiers.Values.CombatHard,
            });
            Assert.Contains("enemydamage 150", hard.StoredKeys, StringComparer.OrdinalIgnoreCase);
            Assert.NotEmpty(normal.ChangesFrom(hard.StoredKeys)); // starting with all Normal asks first

            var reset = await BootAndStopAsync(worldName);

            Assert.Empty(WithoutPresetMarker(reset.StoredKeys));
            Assert.Empty(normal.ChangesFrom(reset.StoredKeys)); // the next start does not ask again
            Assert.Contains(reset.Log, line => line.Contains("Resetting world modifiers"));
        }

        // The game's name for each preset, and VSG's. VSG's Normal is the game's "default".
        public static TheoryData<string, string> Presets => new()
        {
            { "default", WorldGenPresets.Normal },
            { "easy", WorldGenPresets.Easy },
            { "hard", WorldGenPresets.Hard },
            { "hardcore", WorldGenPresets.Hardcore },
            { "casual", WorldGenPresets.Casual },
            { "hammer", WorldGenPresets.Hammer },
            { "immersive", WorldGenPresets.Immersive },
        };

        [Theory]
        [MemberData(nameof(Presets))]
        public async Task A_game_preset_stores_what_VSG_expands_it_to(string gamePreset, string vsgPreset)
        {
            var run = await BootAndStopAsync(_presetWorld.WorldName, o => o.AdditionalArgs = "-preset " + gamePreset);

            Assert.Equal(
                WithoutPresetMarker(WorldGenPresets.Find(vsgPreset)!.Settings.ToGameKeys()),
                WithoutPresetMarker(run.StoredKeys));
        }
    }
}
