using System;
using System.IO;
using ValheimServerGUI.Game;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Game
{
    /// <summary>
    /// World detection against a temp-dir fixture (E11-E15, E20). Both formats (legacy "&lt;World&gt;.fwl"
    /// and Valheim 1.0+ "&lt;World&gt;/*.fwl2"), backup exclusion, cross-folder dedup, non-recursive
    /// .fwl2, and case-insensitivity. §15 #4: availability derives from the same backup-excluded
    /// enumeration, so a backup file never makes a name unavailable.
    /// </summary>
    public class WorldDetectionTests : IDisposable
    {
        private readonly DirectoryInfo SaveFolder;
        private readonly string WorldsLocal;
        private readonly string Worlds;

        public WorldDetectionTests()
        {
            SaveFolder = new DirectoryInfo(Path.Join(Path.GetTempPath(), "vsg_test_" + Guid.NewGuid().ToString("N")));
            WorldsLocal = Path.Join(SaveFolder.FullName, "worlds_local");
            Worlds = Path.Join(SaveFolder.FullName, "worlds");
            Directory.CreateDirectory(WorldsLocal);
            Directory.CreateDirectory(Worlds);
        }

        public void Dispose()
        {
            if (SaveFolder.Exists) SaveFolder.Delete(true);
        }

        private void WriteLegacyWorld(string folder, string name) =>
            File.WriteAllText(Path.Join(folder, $"{name}.fwl"), "");

        private void WriteFolderWorld(string folder, string name)
        {
            var dir = Directory.CreateDirectory(Path.Join(folder, name));
            File.WriteAllText(Path.Join(dir.FullName, "_main.1.fwl2"), "");
        }

        // E14: legacy .fwl files and new-format folders are listed uniformly.
        [Fact]
        public void GetWorldNames_ReturnsLegacyAndFolderWorlds()
        {
            WriteLegacyWorld(WorldsLocal, "LegacyWorld");
            WriteFolderWorld(WorldsLocal, "FolderWorld");

            var names = SaveFolder.GetWorldNames();

            Assert.Contains("LegacyWorld", names);
            Assert.Contains("FolderWorld", names);
        }

        // E12: both backup naming schemes are excluded from the listing.
        [Theory]
        [InlineData("MyWorld_backup_20240604-183113")]
        [InlineData("MyWorld_backup_auto-20240205174838")]
        public void GetWorldNames_ExcludesAutoBackups(string backupName)
        {
            WriteLegacyWorld(WorldsLocal, "MyWorld");
            WriteLegacyWorld(WorldsLocal, backupName);

            var names = SaveFolder.GetWorldNames();

            Assert.Contains("MyWorld", names);
            Assert.DoesNotContain(backupName, names);
        }

        // E13 / §15 #6: a world present in both "worlds" and "worlds_local" is one world, not two.
        [Fact]
        public void GetWorldNames_DedupsAcrossWorldsAndWorldsLocal()
        {
            WriteFolderWorld(Worlds, "SharedWorld");
            WriteFolderWorld(WorldsLocal, "SharedWorld");

            var names = SaveFolder.GetWorldNames();

            Assert.Single(names, n => n == "SharedWorld");
        }

        // E15: a .fwl2 nested below the world folder does not count (non-recursive).
        [Fact]
        public void GetWorldNames_DoesNotCountNestedFwl2()
        {
            var nested = Directory.CreateDirectory(Path.Join(WorldsLocal, "Outer", "sub"));
            File.WriteAllText(Path.Join(nested.FullName, "_main.1.fwl2"), "");

            Assert.DoesNotContain("Outer", SaveFolder.GetWorldNames());
        }

        [Fact]
        public void GetWorldNames_ReturnsEmpty_WhenNoWorldsFolder()
        {
            var empty = new DirectoryInfo(Path.Join(SaveFolder.FullName, "nonexistent"));
            Assert.Empty(empty.GetWorldNames());
        }

        // E11: a cache-only folder (minimap cache, no .fwl2) is not a world; the name stays available.
        [Fact]
        public void IsWorldNameAvailable_TrueForCacheOnlyFolder()
        {
            var dir = Directory.CreateDirectory(Path.Join(WorldsLocal, "CloudWorld"));
            File.WriteAllText(Path.Join(dir.FullName, "cacheMinimapMeta"), "");

            Assert.True(SaveFolder.IsWorldNameAvailable("CloudWorld"));
        }

        [Fact]
        public void IsWorldNameAvailable_FalseWhenWorldFilesPresent()
        {
            WriteFolderWorld(WorldsLocal, "RealWorld");

            Assert.False(SaveFolder.IsWorldNameAvailable("RealWorld"));
        }

        // E20: the same world differing only in case is one world; the name is unavailable either way.
        [Fact]
        public void IsWorldNameAvailable_IsCaseInsensitive()
        {
            WriteFolderWorld(WorldsLocal, "MyWorld");

            Assert.False(SaveFolder.IsWorldNameAvailable("myworld"));
        }

        // §15 #4: a backup file is not a world, so it must not make the base name unavailable -- the
        // enumeration and the availability check agree.
        [Fact]
        public void IsWorldNameAvailable_BackupNameDoesNotBlockItsOwnName()
        {
            WriteLegacyWorld(WorldsLocal, "Loot_backup_auto-20240205174838");

            // The backup is excluded from enumeration, so its name reads as available.
            Assert.True(SaveFolder.IsWorldNameAvailable("Loot_backup_auto-20240205174838"));
        }
    }
}
