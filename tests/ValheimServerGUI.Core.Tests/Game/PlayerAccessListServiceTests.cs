using System;
using System.IO;
using System.Linq;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools.Models;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Game
{
    /// <summary>
    /// Admin/ban/permit list management against a temp-dir savedir. Pins the game's matching semantics
    /// (<c>ZNet.ListContainsId</c>): Steam bare-or-prefixed with dedupe; non-Steam exact case-sensitive on
    /// the raw platform token; header + unauthored line preservation; atomic write.
    /// </summary>
    public class PlayerAccessListServiceTests : IDisposable
    {
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
        private string[] AdminLines() => File.ReadAllLines(AdminPath);

        // ---- path helpers ----

        [Fact]
        public void PathHelpers_ResolveToSavedirRoot()
        {
            Assert.Equal(Path.Join(_savedir, "adminlist.txt"), _saveFolder.GetAdminListFile().FullName);
            Assert.Equal(Path.Join(_savedir, "bannedlist.txt"), _saveFolder.GetBannedListFile().FullName);
            Assert.Equal(Path.Join(_savedir, "permittedlist.txt"), _saveFolder.GetPermittedListFile().FullName);
        }

        // ---- add / header creation ----

        [Fact]
        public void Add_CreatesFileWithGameHeader_AndCanonicalSteamEntry()
        {
            Assert.True(_svc.Add(_savedir, PlayerAccessList.Admin, Steam("76561198000000001")));

            var lines = AdminLines();
            Assert.Equal("// List admin players ID  ONE per line", lines[0]); // exact game header (two spaces)
            Assert.Contains("Steam_76561198000000001", lines);
        }

        [Theory]
        [InlineData(PlayerAccessList.Banned, "// List banned players ID  ONE per line")]
        [InlineData(PlayerAccessList.Permitted, "// List permitted players ID ONE per line")]
        public void Add_UsesExactHeaderPerList(PlayerAccessList list, string expectedHeader)
        {
            _svc.Add(_savedir, list, Steam("1"));
            var file = list == PlayerAccessList.Banned
                ? Path.Join(_savedir, "bannedlist.txt")
                : Path.Join(_savedir, "permittedlist.txt");
            Assert.Equal(expectedHeader, File.ReadAllLines(file)[0]);
        }

        [Fact]
        public void Add_NonSteam_WritesRawTokenVerbatim_CaseSensitive()
        {
            // The binary may carry ambiguous casing ("Playstation"); we write the raw token exactly.
            _svc.Add(_savedir, PlayerAccessList.Admin, NonSteam(PlayerPlatforms.PlayStation, "abc", raw: "Playstation"));

            Assert.Contains("Playstation_abc", AdminLines());
        }

        [Fact]
        public void Add_ReturnsFalse_WhenAlreadyPresent()
        {
            Assert.True(_svc.Add(_savedir, PlayerAccessList.Admin, Steam("1")));
            Assert.False(_svc.Add(_savedir, PlayerAccessList.Admin, Steam("1")));
            Assert.Single(AdminLines().Where(l => l.Contains('1'))); // not duplicated
        }

        // ---- matching (ListContainsId) ----

        [Fact]
        public void Contains_Steam_MatchesBareAndPrefixed()
        {
            File.WriteAllText(AdminPath, "// header\n76561198000000009\n");
            Assert.True(_svc.Contains(_savedir, PlayerAccessList.Admin, Steam("76561198000000009"))); // bare

            File.WriteAllText(AdminPath, "// header\nSteam_76561198000000009\n");
            Assert.True(_svc.Contains(_savedir, PlayerAccessList.Admin, Steam("76561198000000009"))); // prefixed
        }

        [Fact]
        public void Contains_NonSteam_IsExactCaseSensitive()
        {
            _svc.Add(_savedir, PlayerAccessList.Admin, NonSteam(PlayerPlatforms.Xbox, "XUID1"));

            Assert.True(_svc.Contains(_savedir, PlayerAccessList.Admin, NonSteam(PlayerPlatforms.Xbox, "XUID1")));
            // A differently-cased token is a different ID to the game.
            Assert.False(_svc.Contains(_savedir, PlayerAccessList.Admin, NonSteam(PlayerPlatforms.Xbox, "XUID1", raw: "xbox")));
        }

        // ---- dedupe / remove ----

        [Fact]
        public void Add_Steam_DedupesAgainstBareForm()
        {
            File.WriteAllText(AdminPath, "// header\n42\n"); // bare already present
            Assert.False(_svc.Add(_savedir, PlayerAccessList.Admin, Steam("42")));
            Assert.DoesNotContain("Steam_42", AdminLines());
        }

        [Fact]
        public void Remove_Steam_DropsBothBareAndPrefixedForms()
        {
            File.WriteAllText(AdminPath, "// header\n42\nSteam_42\nSteam_99\n");

            Assert.True(_svc.Remove(_savedir, PlayerAccessList.Admin, Steam("42")));

            var lines = AdminLines();
            Assert.DoesNotContain("42", lines);
            Assert.DoesNotContain("Steam_42", lines);
            Assert.Contains("Steam_99", lines); // an unrelated entry survives
        }

        [Fact]
        public void Remove_ReturnsFalse_WhenFileMissingOrNoMatch()
        {
            Assert.False(_svc.Remove(_savedir, PlayerAccessList.Admin, Steam("1"))); // no file yet
            _svc.Add(_savedir, PlayerAccessList.Admin, Steam("1"));
            Assert.False(_svc.Remove(_savedir, PlayerAccessList.Admin, Steam("2"))); // present but no match
        }

        // ---- preservation ----

        [Fact]
        public void Write_PreservesHeaderAndUnauthoredLines()
        {
            File.WriteAllText(AdminPath, "// custom header\n// a hand comment\n999\n");

            _svc.Add(_savedir, PlayerAccessList.Admin, Steam("111"));

            var lines = AdminLines();
            Assert.Equal("// custom header", lines[0]);
            Assert.Contains("// a hand comment", lines);
            Assert.Contains("999", lines);
            Assert.Contains("Steam_111", lines);
        }

        [Fact]
        public void ReadEntries_ExcludesCommentsAndBlanks()
        {
            File.WriteAllText(AdminPath, "// header\n\n  \n// note\nSteam_1\n2\n");

            var entries = _svc.ReadEntries(_savedir, PlayerAccessList.Admin);

            Assert.Equal(new[] { "Steam_1", "2" }, entries);
        }

        [Fact]
        public void ReadEntries_EmptyWhenFileMissing() =>
            Assert.Empty(_svc.ReadEntries(_savedir, PlayerAccessList.Banned));

        [Fact]
        public void Write_IsAtomic_NoTempFileLeftBehind()
        {
            _svc.Add(_savedir, PlayerAccessList.Admin, Steam("1"));
            Assert.False(File.Exists(AdminPath + ".tmp"));
        }

        // ---- round-trip ----

        [Fact]
        public void AddThenRemove_RoundTrips()
        {
            var player = NonSteam(PlayerPlatforms.Nintendo, "N1", raw: "Switch");

            Assert.True(_svc.Add(_savedir, PlayerAccessList.Permitted, player));
            Assert.True(_svc.Contains(_savedir, PlayerAccessList.Permitted, player));
            Assert.True(_svc.Remove(_savedir, PlayerAccessList.Permitted, player));
            Assert.False(_svc.Contains(_savedir, PlayerAccessList.Permitted, player));
        }

        // ---- GenerateFiles (profile roles -> the three files) ----

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

        [Fact]
        public void Generate_OpenMode_RoutesRolesAndWritesCanonicalTokens()
        {
            var roles = new[]
            {
                (Steam("76561198000000001"), PlayerRole.Admin),
                (NonSteam(PlayerPlatforms.PlayStation, "abc", raw: "Playstation"), PlayerRole.Banned),
                (Steam("999"), PlayerRole.Permitted), // permitted is a no-op in open mode
            };

            _svc.GenerateFiles(_savedir, roles.Select(r => ((PlayerInfo)r.Item1, r.Item2)), usePermittedList: false);

            // Admin: canonical Steam token; header preserved.
            Assert.Equal("// List admin players ID  ONE per line", Lines(PlayerAccessList.Admin)[0]);
            Assert.Contains("Steam_76561198000000001", Lines(PlayerAccessList.Admin));
            // Banned: non-Steam raw token verbatim.
            Assert.Contains("Playstation_abc", Lines(PlayerAccessList.Banned));
            // Permitted list is unused in open mode -> header only.
            Assert.Equal(new[] { "// List permitted players ID ONE per line" }, Lines(PlayerAccessList.Permitted));
        }

        [Fact]
        public void Generate_PermittedMode_PutsAdminOnBothLists_AndBannedNowhere()
        {
            var roles = new[]
            {
                (Steam("1"), PlayerRole.Admin),
                (Steam("2"), PlayerRole.Permitted),
                (Steam("3"), PlayerRole.Banned),
            };

            _svc.GenerateFiles(_savedir, roles.Select(r => ((PlayerInfo)r.Item1, r.Item2)), usePermittedList: true);

            Assert.Contains("Steam_1", Lines(PlayerAccessList.Admin));
            // The admin must also be on the permitted list, alongside the permitted player.
            Assert.Contains("Steam_1", Lines(PlayerAccessList.Permitted));
            Assert.Contains("Steam_2", Lines(PlayerAccessList.Permitted));
            // The ban list is ignored in permitted mode -> header only, no members.
            Assert.Equal(new[] { "// List banned players ID  ONE per line" }, Lines(PlayerAccessList.Banned));
        }

        [Fact]
        public void Generate_EmptyConfig_WritesHeaderOnlyFiles()
        {
            _svc.GenerateFiles(_savedir, System.Array.Empty<(PlayerInfo, PlayerRole)>(), usePermittedList: false);

            Assert.Equal(new[] { "// List admin players ID  ONE per line" }, Lines(PlayerAccessList.Admin));
            Assert.Equal(new[] { "// List banned players ID  ONE per line" }, Lines(PlayerAccessList.Banned));
            Assert.Equal(new[] { "// List permitted players ID ONE per line" }, Lines(PlayerAccessList.Permitted));
        }

        // Flipping usePermittedList must move an admin in/out of permittedlist.txt and empty the now-unused
        // list to header-only, purely from a regenerate (no manual file edits).
        [Fact]
        public void Generate_FlippingMode_MovesAdminAcrossPermittedList()
        {
            var roles = new[] { ((PlayerInfo)Steam("1"), PlayerRole.Admin) };

            _svc.GenerateFiles(_savedir, roles, usePermittedList: false);
            Assert.Single(Lines(PlayerAccessList.Permitted)); // header only — admin not on permitted list

            _svc.GenerateFiles(_savedir, roles, usePermittedList: true);
            Assert.Contains("Steam_1", Lines(PlayerAccessList.Permitted)); // now permitted too

            _svc.GenerateFiles(_savedir, roles, usePermittedList: false);
            Assert.Single(Lines(PlayerAccessList.Permitted)); // back to header only
        }

        [Fact]
        public void Generate_OverwritesPriorContent()
        {
            // A stale file with hand-entered members.
            File.WriteAllText(AdminPath, "// old header\nSteam_stale\n999\n");

            _svc.GenerateFiles(_savedir,
                new[] { ((PlayerInfo)Steam("1"), PlayerRole.Admin) }, usePermittedList: false);

            var lines = Lines(PlayerAccessList.Admin);
            Assert.Equal("// List admin players ID  ONE per line", lines[0]); // reset to the game header
            Assert.DoesNotContain("Steam_stale", lines);
            Assert.DoesNotContain("999", lines);
            Assert.Contains("Steam_1", lines);
        }
    }
}
