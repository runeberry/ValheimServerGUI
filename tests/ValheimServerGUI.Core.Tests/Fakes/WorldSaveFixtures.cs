using System;
using System.Collections.Generic;
using System.IO;
using ValheimServerGUI.Game;

namespace ValheimServerGUI.Core.Tests.Fakes
{
    /// <summary>
    /// World save (<c>.fwl2</c>) fixtures: the real game-written files under <c>Fixtures/WorldSaves</c>, and a writer
    /// that mirrors the game's <c>World.SaveWorldFWLData</c> for layouts the real files don't cover.
    /// </summary>
    public static class WorldSaveFixtures
    {
        /// <summary>A real save with a Steam and an Xbox player (identities replaced by same-length fakes).</summary>
        public const string TwoPlatforms = "another_world.fwl2";

        /// <summary>A real save with one Steam player and a starting global key (identities replaced).</summary>
        public const string WithGlobalKey = "TWReleaseWorld.fwl2";

        public static byte[] ReadFixture(string name)
            => File.ReadAllBytes(Path.Join(AppContext.BaseDirectory, "Fixtures", "WorldSaves", name));

        /// <summary>
        /// Writes a save the way the game does. <paramref name="extraField"/> is written just before the player
        /// history, to simulate a future version that adds a field.
        /// </summary>
        public static byte[] Build(
            IEnumerable<WorldPlayerHistoryEntry> history,
            int version = WorldPlayerHistory.PlayerHistoryVersion,
            IReadOnlyList<string>? globalKeys = null,
            Action<BinaryWriter>? extraField = null)
        {
            using var payload = new MemoryStream();
            using (var writer = new BinaryWriter(payload))
            {
                writer.Write(version);
                writer.Write("TestWorld");
                writer.Write("seedName");
                writer.Write(12345);
                writer.Write(67890L);
                writer.Write(2); // worldGenVersion
                writer.Write(true); // needsDB
                globalKeys ??= Array.Empty<string>();
                writer.Write(globalKeys.Count);
                foreach (var key in globalKeys) writer.Write(key);
                extraField?.Invoke(writer);

                var entries = new List<WorldPlayerHistoryEntry>(history);
                writer.Write(entries.Count);
                foreach (var entry in entries)
                {
                    writer.Write(entry.PlatformUserId);
                    writer.Write(entry.DisplayName);
                    writer.Write(entry.ServerAssignedDisplayName);
                    writer.Write(entry.PlayFabId);
                }
            }

            var bytes = payload.ToArray();
            using var file = new MemoryStream();
            using (var writer = new BinaryWriter(file))
            {
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
            return file.ToArray();
        }

        /// <summary>Writes <c>_main.&lt;number&gt;.fwl2</c>, plus its <c>.ok</c> commit marker when <paramref name="committed"/>.</summary>
        public static void WriteSave(DirectoryInfo worldFolder, ulong number, byte[] fwl2, bool committed = true)
        {
            worldFolder.Create();
            File.WriteAllBytes(Path.Join(worldFolder.FullName, $"_main.{number}.fwl2"), fwl2);
            if (committed)
            {
                File.WriteAllBytes(Path.Join(worldFolder.FullName, $"_main.{number}.ok"), BitConverter.GetBytes(41));
            }
        }
    }
}
