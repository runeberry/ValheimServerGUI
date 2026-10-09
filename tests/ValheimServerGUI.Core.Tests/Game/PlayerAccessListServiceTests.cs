using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools.Models;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Game
{
    /// <summary>
    /// The three list files against a temp-dir savedir: reading, the role → file routing rules, and the write safety
    /// rules (lines VSG doesn't manage are carried through; a file losing an entry VSG didn't write is backed up first;
    /// a failed backup writes nothing).
    /// </summary>
    public class PlayerAccessListServiceTests : IDisposable
    {
        private const string AdminHeader = "// List admin players ID  ONE per line";
        private const string BannedHeader = "// List banned players ID  ONE per line";
        private const string PermittedHeader = "// List permitted players ID ONE per line";

        private readonly DirectoryInfo _saveFolder;
        private readonly string _savedir;
        private readonly PlayerAccessListService _svc = new();

        public PlayerAccessListServiceTests()
        {
            _saveFolder = new DirectoryInfo(Path.Join(Path.GetTempPath(), "vsg_acl_" + Guid.NewGuid().ToString("N")));
            _saveFolder.Create();
            _savedir = _saveFolder.FullName;
        }

        public void Dispose()
        {
            if (_saveFolder.Exists) _saveFolder.Delete(true);
        }

        private static PlayerInfo Steam(string id) => new() { Platform = PlayerPlatforms.Steam, PlayerId = id };

        private static PlayerInfo NonSteam(string platform, string id, string? raw = null)
            => new() { Platform = platform, PlatformRaw = raw ?? platform, PlayerId = id };

        private string AdminPath => Path.Join(_savedir, "adminlist.txt");

        private string[] Lines(PlayerAccessList list)
        {
            var name = list switch
            {
                PlayerAccessList.Admin => "adminlist.txt",
                PlayerAccessList.Banned => "bannedlist.txt",
                _ => "permittedlist.txt",
            };
            return File.ReadAllLines(Path.Join(_savedir, name));
        }

        private PlayerListWriteResult Write(
            bool usePermittedList, PlayerListBaseline? baseline, params (PlayerInfo player, PlayerRole role)[] roles)
            => _svc.WriteLists(_savedir, roles, usePermittedList, baseline);

        private static PlayerListBaseline Baseline(PlayerAccessList list, params string[] entries)
            => new(new Dictionary<PlayerAccessList, IReadOnlyList<string>> { [list] = entries });

        private string[] Backups(string stem) => Directory.GetFiles(_savedir, stem + ".bak*.txt");

        // ---- path helpers + reading ----

        [Fact]
        public void PathHelpers_ResolveToSavedirRoot()
        {
            Assert.Equal(Path.Join(_savedir, "adminlist.txt"), _saveFolder.GetAdminListFile().FullName);
            Assert.Equal(Path.Join(_savedir, "bannedlist.txt"), _saveFolder.GetBannedListFile().FullName);
            Assert.Equal(Path.Join(_savedir, "permittedlist.txt"), _saveFolder.GetPermittedListFile().FullName);
        }

        [Fact]
        public void ReadEntries_ExcludesCommentsAndBlanks()
        {
            File.WriteAllText(AdminPath, "// header\n\n  \n// note\nSteam_1\n2\n");

            Assert.Equal(new[] { "Steam_1", "2" }, _svc.ReadEntries(_savedir, PlayerAccessList.Admin));
        }

        [Fact]
        public void ReadEntries_EmptyWhenFileMissing() =>
            Assert.Empty(_svc.ReadEntries(_savedir, PlayerAccessList.Banned));

        // ---- routing: profile roles -> the three files ----

        [Fact]
        public void Write_OpenMode_RoutesRolesAndWritesCanonicalTokens()
        {
            Write(false, null,
                (Steam("76561198000000001"), PlayerRole.Admin),
                (NonSteam(PlayerPlatforms.PlayStation, "abc", raw: "Playstation"), PlayerRole.Banned),
                (Steam("999"), PlayerRole.Permitted)); // permitted is a no-op in open mode

            Assert.Equal(AdminHeader, Lines(PlayerAccessList.Admin)[0]); // the game's exact header for a new file
            Assert.Contains("Steam_76561198000000001", Lines(PlayerAccessList.Admin));
            Assert.Contains("Playstation_abc", Lines(PlayerAccessList.Banned)); // non-Steam raw token verbatim
            Assert.Equal(new[] { PermittedHeader }, Lines(PlayerAccessList.Permitted));
        }

        [Fact]
        public void Write_PermittedMode_PutsAdminOnBothLists_AndBannedNowhere()
        {
            Write(true, null,
                (Steam("1"), PlayerRole.Admin),
                (Steam("2"), PlayerRole.Permitted),
                (Steam("3"), PlayerRole.Banned));

            Assert.Contains("Steam_1", Lines(PlayerAccessList.Admin));
            Assert.Contains("Steam_1", Lines(PlayerAccessList.Permitted)); // an admin must also be permitted
            Assert.Contains("Steam_2", Lines(PlayerAccessList.Permitted));
            Assert.Equal(new[] { BannedHeader }, Lines(PlayerAccessList.Banned)); // ignored in permitted mode
        }

        [Fact]
        public void Write_NoRoles_WritesHeaderOnlyFiles()
        {
            Write(false, null);

            Assert.Equal(new[] { AdminHeader }, Lines(PlayerAccessList.Admin));
            Assert.Equal(new[] { BannedHeader }, Lines(PlayerAccessList.Banned));
            Assert.Equal(new[] { PermittedHeader }, Lines(PlayerAccessList.Permitted));
        }

        // Flipping usePermittedList moves an admin in/out of permittedlist.txt. VSG wrote that entry itself, so
        // dropping it again (with the previous write as the baseline) needs no backup.
        [Fact]
        public void Write_FlippingMode_MovesAdminAcrossPermittedList_WithoutBackups()
        {
            var role = (Steam("1"), PlayerRole.Admin);

            var first = Write(false, null, role);
            Assert.Single(Lines(PlayerAccessList.Permitted));

            var second = Write(true, first.Written, role);
            Assert.Contains("Steam_1", Lines(PlayerAccessList.Permitted));

            Write(false, second.Written, role);
            Assert.Single(Lines(PlayerAccessList.Permitted));
            Assert.Empty(Backups("permittedlist"));
        }

        [Fact]
        public void Write_ReturnsWhatItWrote_AsTheNextBaseline()
        {
            File.WriteAllText(AdminPath, "// header\nPlayFab_X1\n");

            var result = Write(false, null, (Steam("1"), PlayerRole.Admin));

            Assert.Equal(new[] { "PlayFab_X1", "Steam_1" }, result.Written.For(PlayerAccessList.Admin));
            Assert.Empty(result.Written.For(PlayerAccessList.Banned));
        }

        [Fact]
        public void Write_LeavesNoTempFileBehind()
        {
            Write(false, null, (Steam("1"), PlayerRole.Admin));
            Assert.Empty(Directory.GetFiles(_savedir, "*.tmp"));
        }

        // ---- safety: never lose an entry VSG didn't write ----

        [Fact]
        public void Write_KeepsCommentsAndUnrecognizedEntries_Verbatim()
        {
            File.WriteAllText(AdminPath, "// custom header\n// Bob's alt\nPlayFab_1A2B\nnot a player\nSteam_1\n");

            Write(false, null, (Steam("1"), PlayerRole.Admin));

            var lines = Lines(PlayerAccessList.Admin);
            Assert.Equal("// custom header", lines[0]);
            Assert.Contains("// Bob's alt", lines);
            Assert.Contains("PlayFab_1A2B", lines);
            Assert.Contains("not a player", lines);
            Assert.Contains("Steam_1", lines);
            Assert.Empty(Backups("adminlist")); // nothing was dropped
        }

        [Fact]
        public void Write_DroppingAnEntryVsgDidNotWrite_BacksTheFileUpFirst()
        {
            File.WriteAllText(AdminPath, "// header\nSteam_76561198000000009\n"); // written by someone else

            var result = Write(false, null);

            Assert.DoesNotContain("Steam_76561198000000009", Lines(PlayerAccessList.Admin));
            var backup = Assert.Single(result.Backups);
            Assert.Contains("Steam_76561198000000009", File.ReadAllLines(backup.FullName));
        }

        [Fact]
        public void Write_DroppingAnEntryVsgWroteItself_NeedsNoBackup()
        {
            File.WriteAllText(AdminPath, "// header\nSteam_76561198000000009\n");

            var result = Write(false, Baseline(PlayerAccessList.Admin, "Steam_76561198000000009"));

            Assert.DoesNotContain("Steam_76561198000000009", Lines(PlayerAccessList.Admin));
            Assert.Empty(result.Backups);
        }

        // A/B partner of the test above: the same drop, with the entry missing from the baseline, is backed up. So the
        // baseline really is what decides, not the drop itself.
        [Fact]
        public void Write_BaselineForAnotherPlayer_StillBacksUp()
        {
            File.WriteAllText(AdminPath, "// header\nSteam_76561198000000009\n");

            var result = Write(false, Baseline(PlayerAccessList.Admin, "Steam_76561198000000001"));

            Assert.Single(result.Backups);
        }

        [Fact]
        public void Write_BareAndPrefixedSteam_AreTheSamePlayer_SoRewritingIsNotADrop()
        {
            File.WriteAllText(AdminPath, "// header\n76561198000000009\n");

            var result = Write(false, null, (Steam("76561198000000009"), PlayerRole.Admin));

            Assert.Contains("Steam_76561198000000009", Lines(PlayerAccessList.Admin));
            Assert.Empty(result.Backups);
        }

        [Fact]
        public void Write_WhenABackupFails_ThrowsAndWritesNothing()
        {
            File.WriteAllText(AdminPath, "// header\nSteam_76561198000000009\n");
            var bannedPath = Path.Join(_savedir, "bannedlist.txt");
            File.WriteAllText(bannedPath, "// header\n");
            // Directories squatting on every backup name the writer would try make the backup impossible.
            Directory.CreateDirectory(Path.Join(_savedir, "adminlist.bak.txt"));
            for (var i = 2; i <= 5; i++) Directory.CreateDirectory(Path.Join(_savedir, $"adminlist.bak.{i}.txt"));
            var adminBefore = File.ReadAllText(AdminPath);

            var ex = Assert.Throws<PlayerListBackupException>(() => Write(false, null, (Steam("1"), PlayerRole.Banned)));

            Assert.Equal(AdminPath, ex.FilePath);
            Assert.Equal(adminBefore, File.ReadAllText(AdminPath));     // not rewritten
            Assert.Equal("// header\n", File.ReadAllText(bannedPath));  // no other file touched either
        }
    }
}
