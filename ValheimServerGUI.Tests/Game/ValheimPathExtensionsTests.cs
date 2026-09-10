using System;
using System.IO;
using System.Linq;
using ValheimServerGUI.Game;
using Xunit;

namespace ValheimServerGUI.Tests.Game
{
    public class ValheimPathExtensionsTests : IDisposable
    {
        private readonly DirectoryInfo SaveFolder;
        private readonly string WorldsLocal;

        public ValheimPathExtensionsTests()
        {
            SaveFolder = new DirectoryInfo(Path.Join(Path.GetTempPath(), "vsg_test_" + Guid.NewGuid().ToString("N")));
            WorldsLocal = Path.Join(SaveFolder.FullName, "worlds_local");
            Directory.CreateDirectory(WorldsLocal);
        }

        public void Dispose()
        {
            if (SaveFolder.Exists) SaveFolder.Delete(true);
        }

        private void WriteLegacyWorld(string name) => File.WriteAllText(Path.Join(WorldsLocal, $"{name}.fwl"), "");

        private void WriteFolderWorld(string name)
        {
            var dir = Directory.CreateDirectory(Path.Join(WorldsLocal, name));
            File.WriteAllText(Path.Join(dir.FullName, "_main.1.fwl2"), "");
        }

        [Fact]
        public void GetWorldNames_ReturnsLegacyAndFolderWorlds()
        {
            WriteLegacyWorld("LegacyWorld");
            WriteFolderWorld("FolderWorld");

            var names = SaveFolder.GetWorldNames();

            Assert.Contains("LegacyWorld", names);
            Assert.Contains("FolderWorld", names);
        }

        [Theory]
        // "_backup_<date>-<time>" scheme
        [InlineData("MyWorld_backup_20240604-183113")]
        // "_backup_auto-<timestamp>" scheme (Steam Cloud legacy auto-backups)
        [InlineData("MyWorld_backup_auto-20240205174838")]
        public void GetWorldNames_ExcludesAutoBackups(string backupName)
        {
            WriteLegacyWorld("MyWorld");
            WriteLegacyWorld(backupName);

            var names = SaveFolder.GetWorldNames();

            Assert.Contains("MyWorld", names);
            Assert.DoesNotContain(backupName, names);
        }

        [Fact]
        public void GetWorldNames_ReturnsEmpty_WhenNoWorldsFolder()
        {
            var empty = new DirectoryInfo(Path.Join(SaveFolder.FullName, "nonexistent"));
            Assert.Empty(empty.GetWorldNames());
        }
    }
}
