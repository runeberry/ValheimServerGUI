using System;
using System.IO;
using ValheimServerGUI.Game;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Game
{
    /// <summary>
    /// <see cref="ValheimPathExtensions.BackupListFile"/>: increments to the first free ".bak" slot and returns
    /// null (rather than throwing) when the move cannot be performed.
    /// </summary>
    public class BackupListFileTests : IDisposable
    {
        private readonly DirectoryInfo _dir;

        public BackupListFileTests()
        {
            _dir = new DirectoryInfo(Path.Join(Path.GetTempPath(), "vsg_bak_" + Guid.NewGuid().ToString("N")));
            _dir.Create();
        }

        public void Dispose()
        {
            if (_dir.Exists) _dir.Delete(true);
        }

        private FileInfo Write(string name, string content)
        {
            var path = Path.Join(_dir.FullName, name);
            File.WriteAllText(path, content);
            return new FileInfo(path);
        }

        [Fact]
        public void FirstBackup_UsesUnnumberedBakName_AndMovesContent()
        {
            var file = Write("permittedlist.txt", "keep me");

            var dest = ValheimPathExtensions.BackupListFile(file);

            Assert.NotNull(dest);
            Assert.Equal("permittedlist.bak.txt", dest!.Name);
            Assert.False(File.Exists(file.FullName));           // original moved away
            Assert.Equal("keep me", File.ReadAllText(dest.FullName));
        }

        [Fact]
        public void SecondBackup_IncrementsWhenFirstSlotTaken()
        {
            File.WriteAllText(Path.Join(_dir.FullName, "permittedlist.bak.txt"), "old");
            var file = Write("permittedlist.txt", "new");

            var dest = ValheimPathExtensions.BackupListFile(file);

            Assert.NotNull(dest);
            Assert.Equal("permittedlist.bak.2.txt", dest!.Name);
        }

        [Fact]
        public void ReturnsNull_WhenMoveFails()
        {
            var file = Write("permittedlist.txt", "content");
            // Occupy the destination path with a directory so File.Move throws (IOException) rather than succeeds.
            Directory.CreateDirectory(Path.Join(_dir.FullName, "permittedlist.bak.txt"));

            var dest = ValheimPathExtensions.BackupListFile(file);

            Assert.Null(dest);
            Assert.True(File.Exists(file.FullName)); // original left in place for the caller to handle
        }

        [Fact]
        public void ReturnsNull_WhenFileDoesNotExist()
        {
            var missing = new FileInfo(Path.Join(_dir.FullName, "nope.txt"));
            Assert.Null(ValheimPathExtensions.BackupListFile(missing));
        }
    }
}
