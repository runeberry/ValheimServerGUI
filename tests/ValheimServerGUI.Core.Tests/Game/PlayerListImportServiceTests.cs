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
    /// Reconciliation of on-disk list files with a profile's roles: the wholesale <see cref="IPlayerListImportService.BuildImport"/>
    /// and the one-directional start-time <see cref="IPlayerListImportService.CheckConflicts"/>.
    /// </summary>
    public class PlayerListImportServiceTests : IDisposable
    {
        private readonly DirectoryInfo _dir;
        private readonly string _savedir;
        private readonly PlayerListImportService _svc = new(new PlayerAccessListService());

        public PlayerListImportServiceTests()
        {
            _dir = new DirectoryInfo(Path.Join(Path.GetTempPath(), "vsg_import_" + Guid.NewGuid().ToString("N")));
            _dir.Create();
            _savedir = _dir.FullName;
        }

        public void Dispose()
        {
            if (_dir.Exists) _dir.Delete(true);
        }

        private void Seed(string fileName, params string[] entries)
            => File.WriteAllText(Path.Join(_savedir, fileName), "// header\n" + string.Join('\n', entries) + "\n");

        private static Dictionary<string, PlayerRoleEntry> Roles(params (string key, PlayerRole role, string raw)[] roles)
            => roles.ToDictionary(r => r.key, r => new PlayerRoleEntry(r.role, r.raw));

        private const string SteamA = "76561198000000001";
        private const string SteamB = "76561198000000002";

        // ---- BuildImport ----

        [Fact]
        public void BuildImport_NoFiles_ReportsAnyFilesPresentFalse()
        {
            var plan = _svc.BuildImport(_savedir, new Dictionary<string, PlayerRoleEntry>(), currentFlag: false);

            Assert.True(plan.Success);
            Assert.False(plan.AnyFilesPresent);
            Assert.Equal(0, plan.UpdateCount);
        }

        [Fact]
        public void BuildImport_DerivesUsePermittedList_FromNonEmptyPermittedList()
        {
            Seed("permittedlist.txt", SteamA);

            var plan = _svc.BuildImport(_savedir, new Dictionary<string, PlayerRoleEntry>(), currentFlag: false);

            Assert.True(plan.Success);
            Assert.True(plan.UsePermittedList);
            Assert.Equal(PlayerRole.Permitted, plan.Roles[$"Steam:{SteamA}"].Role);
        }

        [Fact]
        public void BuildImport_RolePrecedence_BannedOverAdminOverPermitted()
        {
            // Same player appears in all three files: a ban must win.
            Seed("bannedlist.txt", SteamA);
            Seed("adminlist.txt", SteamA);
            Seed("permittedlist.txt", SteamA);

            var plan = _svc.BuildImport(_savedir, new Dictionary<string, PlayerRoleEntry>(), currentFlag: false);

            Assert.Equal(PlayerRole.Banned, plan.Roles[$"Steam:{SteamA}"].Role);
        }

        [Fact]
        public void BuildImport_LiteralUnset_DropsConfigPlayerAbsentFromFiles()
        {
            Seed("adminlist.txt", SteamA);
            var current = Roles(($"Steam:{SteamB}", PlayerRole.Admin, "Steam")); // B not in any file

            var plan = _svc.BuildImport(_savedir, current, currentFlag: false);

            Assert.True(plan.Roles.ContainsKey($"Steam:{SteamA}"));
            Assert.False(plan.Roles.ContainsKey($"Steam:{SteamB}")); // dropped
            Assert.Equal(2, plan.UpdateCount); // 1 add (A) + 1 remove (B)
        }

        [Fact]
        public void BuildImport_CountsFlagFlip_AsAnUpdate()
        {
            // permittedlist non-empty flips the derived flag from the current false; the roles already match.
            Seed("permittedlist.txt", SteamA);
            var current = Roles(($"Steam:{SteamA}", PlayerRole.Permitted, "Steam"));

            var plan = _svc.BuildImport(_savedir, current, currentFlag: false);

            Assert.True(plan.UsePermittedList);
            Assert.Equal(1, plan.UpdateCount); // only the flag flip
        }

        [Fact]
        public void BuildImport_PopulatesNewPlayers_ForPreviouslyUnknownKeys()
        {
            Seed("adminlist.txt", SteamA);

            var plan = _svc.BuildImport(_savedir, new Dictionary<string, PlayerRoleEntry>(), currentFlag: false);

            var np = Assert.Single(plan.NewPlayers);
            Assert.Equal(PlayerPlatforms.Steam, np.Platform);
            Assert.Equal(SteamA, np.PlayerId);
            Assert.Equal(PlayerRole.Admin, np.Role);
        }

        [Fact]
        public void BuildImport_IsIdempotent_SecondImportHasNoUpdates()
        {
            Seed("adminlist.txt", SteamA);
            Seed("permittedlist.txt", SteamB);

            var first = _svc.BuildImport(_savedir, new Dictionary<string, PlayerRoleEntry>(), currentFlag: false);
            Assert.True(first.UpdateCount > 0);

            // Feed the first result back in as the current state → nothing left to do.
            var second = _svc.BuildImport(_savedir, first.Roles, first.UsePermittedList);
            Assert.Equal(0, second.UpdateCount);
            Assert.Empty(second.NewPlayers);
        }

        [Fact]
        public void BuildImport_UnresolvableToken_FailsClosed()
        {
            Seed("adminlist.txt", "not-a-valid-token");

            var plan = _svc.BuildImport(_savedir, new Dictionary<string, PlayerRoleEntry>(), currentFlag: false);

            Assert.False(plan.Success);
            Assert.NotNull(plan.FailureReason);
        }

        // ---- CheckConflicts ----

        [Fact]
        public void CheckConflicts_WrongRole_IsConflict_WithConflictingFile()
        {
            Seed("adminlist.txt", SteamA);
            var current = Roles(($"Steam:{SteamA}", PlayerRole.Banned, "Steam")); // config says banned, file says admin

            var report = _svc.CheckConflicts(_savedir, current, currentFlag: false);

            Assert.True(report.Success);
            Assert.Equal(1, report.ConflictCount);
            Assert.Contains(PlayerAccessList.Admin, report.ConflictingFiles);
            Assert.Empty(report.Additions);
        }

        [Fact]
        public void CheckConflicts_MissingPlayer_IsAddition_NotConflict()
        {
            Seed("bannedlist.txt", SteamA);

            var report = _svc.CheckConflicts(_savedir, new Dictionary<string, PlayerRoleEntry>(), currentFlag: false);

            Assert.Equal(0, report.ConflictCount);
            var add = Assert.Single(report.Additions);
            Assert.Equal($"Steam:{SteamA}", add.Key);
            Assert.Equal(PlayerRole.Banned, add.Entry.Role);
            Assert.Single(report.NewPlayers);
        }

        [Fact]
        public void CheckConflicts_PermittedList_AcceptsAdminOrPermitted()
        {
            Seed("permittedlist.txt", SteamA, SteamB);
            var current = Roles(
                ($"Steam:{SteamA}", PlayerRole.Admin, "Steam"),
                ($"Steam:{SteamB}", PlayerRole.Permitted, "Steam"));

            var report = _svc.CheckConflicts(_savedir, current, currentFlag: true);

            Assert.Equal(0, report.ConflictCount);
            Assert.Empty(report.Additions);
        }

        [Fact]
        public void CheckConflicts_NeverUnsets_ConfigPlayerAbsentFromFilesIsUntouched()
        {
            Seed("adminlist.txt", SteamA);
            var current = Roles(($"Steam:{SteamB}", PlayerRole.Permitted, "Steam")); // B not in files

            var report = _svc.CheckConflicts(_savedir, current, currentFlag: false);

            // A is missing → addition; B is never referenced (no removal exists in the conflict model).
            Assert.Equal(0, report.ConflictCount);
            Assert.Single(report.Additions);
            Assert.DoesNotContain(report.Additions, a => a.Key == $"Steam:{SteamB}");
        }

        [Fact]
        public void CheckConflicts_UnresolvableToken_FailsClosed()
        {
            Seed("bannedlist.txt", "garbage");

            var report = _svc.CheckConflicts(_savedir, new Dictionary<string, PlayerRoleEntry>(), currentFlag: false);

            Assert.False(report.Success);
            Assert.NotNull(report.FailureReason);
        }

        // ---- HasEntries ----

        [Fact]
        public void HasEntries_TrueOnlyWhenContentLinesPresent()
        {
            Assert.False(_svc.HasEntries(_savedir, PlayerAccessList.Permitted)); // no file
            File.WriteAllText(Path.Join(_savedir, "permittedlist.txt"), "// header only\n");
            Assert.False(_svc.HasEntries(_savedir, PlayerAccessList.Permitted)); // header-only
            Seed("permittedlist.txt", SteamA);
            Assert.True(_svc.HasEntries(_savedir, PlayerAccessList.Permitted));
        }
    }
}
