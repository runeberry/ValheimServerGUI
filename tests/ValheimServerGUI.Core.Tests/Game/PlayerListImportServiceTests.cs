using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ValheimServerGUI.Core.Tests.Fakes;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools.Models;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Game
{
    /// <summary>
    /// Reconciliation of on-disk list files with a profile's roles: the wholesale <see cref="IPlayerListImportService.BuildImport"/>
    /// and <see cref="IPlayerListImportService.Reconcile"/>, which adopts only what changed since VSG's last write.
    /// </summary>
    public class PlayerListImportServiceTests : IDisposable
    {
        private readonly DirectoryInfo _dir;
        private readonly string _savedir;
        private readonly InMemoryBaselineStore _baselines = new();
        private readonly PlayerListImportService _svc;

        public PlayerListImportServiceTests()
        {
            _dir = new DirectoryInfo(Path.Join(Path.GetTempPath(), "vsg_import_" + Guid.NewGuid().ToString("N")));
            _dir.Create();
            _savedir = _dir.FullName;
            _svc = new PlayerListImportService(new PlayerAccessListService(), _baselines);
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

            Assert.False(plan.AnyFilesPresent);
            Assert.Equal(0, plan.UpdateCount);
        }

        [Fact]
        public void BuildImport_DerivesUsePermittedList_FromNonEmptyPermittedList()
        {
            Seed("permittedlist.txt", SteamA);

            var plan = _svc.BuildImport(_savedir, new Dictionary<string, PlayerRoleEntry>(), currentFlag: false);

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
        public void BuildImport_UnrecognizedEntry_IsSkipped_AndTheRestImports()
        {
            Seed("adminlist.txt", "not-a-valid-token", SteamA);

            var plan = _svc.BuildImport(_savedir, new Dictionary<string, PlayerRoleEntry>(), currentFlag: false);

            Assert.Equal(PlayerRole.Admin, plan.Roles[$"Steam:{SteamA}"].Role);
            Assert.Equal("adminlist.txt: not-a-valid-token", Assert.Single(plan.Unrecognized));
        }

        // ---- BuildImport with global defaults ----

        private static Dictionary<string, PlayerDefaultEntry> Defaults(params (string key, PlayerRole role)[] entries)
            => entries.ToDictionary(e => e.key, e => new PlayerDefaultEntry(e.role, "Steam"));

        [Fact]
        public void BuildImport_WithDefaults_FileMatchingTheDefault_NeedsNoOverride()
        {
            Seed("adminlist.txt", SteamA);
            var defaults = Defaults(($"Steam:{SteamA}", PlayerRole.Admin));

            var plan = _svc.BuildImport(_savedir, new Dictionary<string, PlayerRoleEntry>(), currentFlag: false, defaults);

            Assert.Empty(plan.Roles);
            Assert.Equal(0, plan.UpdateCount);
            Assert.Empty(plan.NewPlayers); // already known via the defaults
        }

        [Fact]
        public void BuildImport_WithDefaults_DefaultedPlayerAbsentFromFiles_KeepsTheirDefault()
        {
            Seed("adminlist.txt", SteamB);
            var defaults = Defaults(($"Steam:{SteamA}", PlayerRole.Admin));
            var current = Roles(($"Steam:{SteamA}", PlayerRole.Banned, "Steam")); // an override the files don't back

            var plan = _svc.BuildImport(_savedir, current, currentFlag: false, defaults);

            // A server can't override to "no role": A's override is dropped and A falls back to the default.
            Assert.False(plan.Roles.ContainsKey($"Steam:{SteamA}"));
            Assert.Equal(PlayerRole.Admin, plan.Roles[$"Steam:{SteamB}"].Role);
            Assert.Equal(2, plan.UpdateCount); // A: Banned→Admin (default), B: None→Admin
        }

        [Fact]
        public void BuildImport_WithDefaults_FileDisagreeingWithDefault_BecomesOverride()
        {
            Seed("bannedlist.txt", SteamA);
            var defaults = Defaults(($"Steam:{SteamA}", PlayerRole.Permitted));

            var plan = _svc.BuildImport(_savedir, new Dictionary<string, PlayerRoleEntry>(), currentFlag: false, defaults);

            Assert.Equal(PlayerRole.Banned, Assert.Single(plan.Roles).Value.Role);
        }

        [Fact]
        public void BuildImport_UnlistedPlayerAbsentFromFiles_IsCleared_NotPinned()
        {
            Seed("adminlist.txt", SteamA);
            var current = Roles(($"Steam:{SteamB}", PlayerRole.Permitted, "Steam")); // B has no default

            var plan = _svc.BuildImport(_savedir, current, currentFlag: false, Defaults());

            Assert.False(plan.Roles.ContainsKey($"Steam:{SteamB}"));
        }

        // ---- Reconcile ----

        private static readonly Dictionary<string, PlayerDefaultEntry> NoDefaults = new();

        // VSG's previous write: the files as they were when it last wrote them.
        private void LastWrote(params (PlayerAccessList list, string[] entries)[] lists)
            => _baselines.Save(_savedir, new PlayerListBaseline(
                lists.ToDictionary(l => l.list, l => (IReadOnlyList<string>)l.entries)));

        private ReconcileResult Reconcile(
            Dictionary<string, PlayerRoleEntry> overrides, bool externalWins,
            Dictionary<string, PlayerDefaultEntry>? defaults = null)
            => _svc.Reconcile(_savedir, overrides, defaults ?? NoDefaults, externalWins);

        [Fact]
        public void Reconcile_NoBaseline_EveryEntryCountsAsAdded()
        {
            Seed("bannedlist.txt", SteamA);

            var result = Reconcile(new(), externalWins: false);

            Assert.Equal(PlayerRole.Banned, result.Overrides[$"Steam:{SteamA}"].Role);
            Assert.Single(result.NewPlayers);
            Assert.Empty(result.Conflicts);
        }

        [Fact]
        public void Reconcile_EntriesVsgWroteItself_AreNotChanges()
        {
            Seed("adminlist.txt", SteamA);
            LastWrote((PlayerAccessList.Admin, new[] { $"Steam_{SteamA}" })); // bare vs prefixed: same player
            var overrides = Roles(($"Steam:{SteamA}", PlayerRole.Permitted, "Steam")); // changed in VSG since

            var result = Reconcile(overrides, externalWins: false);

            Assert.False(result.Changed);
            Assert.Empty(result.Conflicts); // VSG's own stale entry is not a conflict
        }

        [Fact]
        public void Reconcile_AddedEntryForAPlayerWithNoRole_IsAdopted_EvenWhenTheProfileWins()
        {
            Seed("bannedlist.txt", SteamA);
            LastWrote();

            var result = Reconcile(new(), externalWins: false);

            Assert.Equal(PlayerRole.Banned, result.Overrides[$"Steam:{SteamA}"].Role);
            Assert.Empty(result.Conflicts);
        }

        [Fact]
        public void Reconcile_AddedEntryThatDisagrees_IsAConflict_AndTheProfileKeepsItsRole()
        {
            Seed("bannedlist.txt", SteamA);
            LastWrote();
            var overrides = Roles(($"Steam:{SteamA}", PlayerRole.Admin, "Steam"));

            var result = Reconcile(overrides, externalWins: false);

            var conflict = Assert.Single(result.Conflicts);
            Assert.Equal((PlayerAccessList.Banned, PlayerRole.Banned, PlayerRole.Admin), (conflict.List, conflict.FileRole, conflict.ProfileRole));
            Assert.Equal(PlayerRole.Admin, result.Overrides[$"Steam:{SteamA}"].Role);
            Assert.False(result.Changed);
        }

        [Fact]
        public void Reconcile_AddedEntryThatDisagrees_WinsWhenExternalChangesWin()
        {
            Seed("bannedlist.txt", SteamA); // e.g. an admin banned in-game
            LastWrote();
            var overrides = Roles(($"Steam:{SteamA}", PlayerRole.Admin, "Steam"));

            var result = Reconcile(overrides, externalWins: true);

            Assert.Equal(PlayerRole.Banned, result.Overrides[$"Steam:{SteamA}"].Role);
            Assert.True(result.Changed);
        }

        [Fact]
        public void Reconcile_PlayerAddedToSeveralLists_TakesBannedOverAdminOverPermitted()
        {
            Seed("adminlist.txt", SteamA);
            Seed("bannedlist.txt", SteamA);
            Seed("permittedlist.txt", SteamA);
            LastWrote();

            var result = Reconcile(new(), externalWins: true);

            Assert.Equal(PlayerRole.Banned, result.Overrides[$"Steam:{SteamA}"].Role);
        }

        [Fact]
        public void Reconcile_PermittedList_IsSatisfiedByAnAdmin()
        {
            Seed("permittedlist.txt", SteamA);
            LastWrote();
            var overrides = Roles(($"Steam:{SteamA}", PlayerRole.Admin, "Steam"));

            var result = Reconcile(overrides, externalWins: true);

            Assert.False(result.Changed);
            Assert.Empty(result.Conflicts);
        }

        [Fact]
        public void Reconcile_ComparesAgainstTheEffectiveRole_FromDefaults()
        {
            Seed("adminlist.txt", SteamA);
            LastWrote();
            var defaults = Defaults(($"Steam:{SteamA}", PlayerRole.Admin));

            var result = Reconcile(new(), externalWins: false, defaults);

            Assert.False(result.Changed); // the default already puts A on the admin list
        }

        [Fact]
        public void Reconcile_RemovedEntry_RemovesTheServerOverride()
        {
            Seed("bannedlist.txt"); // in-game unban: A is gone
            LastWrote((PlayerAccessList.Banned, new[] { $"Steam_{SteamA}" }));
            var overrides = Roles(($"Steam:{SteamA}", PlayerRole.Banned, "Steam"));

            var result = Reconcile(overrides, externalWins: true);

            Assert.False(result.Overrides.ContainsKey($"Steam:{SteamA}"));
            Assert.True(result.Changed);
        }

        [Fact]
        public void Reconcile_RemovedEntry_FallsBackToTheDefault_NotTheOldOverride()
        {
            // An admin (by default) was banned in-game, then unbanned in-game: the ban override goes, the default
            // admin comes back. Only an admin OVERRIDE would have been lost to the ban.
            Seed("bannedlist.txt");
            LastWrote((PlayerAccessList.Banned, new[] { $"Steam_{SteamA}" }));
            var overrides = Roles(($"Steam:{SteamA}", PlayerRole.Banned, "Steam"));
            var defaults = Defaults(($"Steam:{SteamA}", PlayerRole.Admin));

            var result = Reconcile(overrides, externalWins: true, defaults);

            Assert.Equal(PlayerRole.Admin, PlayerRoleResolver.Resolve($"Steam:{SteamA}", result.Overrides, defaults).Effective);
        }

        [Fact]
        public void Reconcile_RemovedEntry_LeavesAGlobalBanAlone()
        {
            Seed("bannedlist.txt");
            LastWrote((PlayerAccessList.Banned, new[] { $"Steam_{SteamA}" }));
            var defaults = Defaults(($"Steam:{SteamA}", PlayerRole.Banned));

            var result = Reconcile(new(), externalWins: true, defaults);

            Assert.False(result.Changed);
            Assert.Contains(result.LogLines, l => l.Contains("global default"));
        }

        [Fact]
        public void Reconcile_RemovedEntry_ForARoleThePlayerNoLongerHas_IsIgnored()
        {
            // VSG wrote A to the ban list, then the user unbanned A in VSG and the file still has... nothing: A was
            // also removed on disk. With no ban role left, there is nothing to undo.
            Seed("bannedlist.txt");
            LastWrote((PlayerAccessList.Banned, new[] { $"Steam_{SteamA}" }));
            var overrides = Roles(($"Steam:{SteamA}", PlayerRole.Permitted, "Steam"));

            var result = Reconcile(overrides, externalWins: true);

            Assert.False(result.Changed);
        }

        [Fact]
        public void Reconcile_SkipsUnrecognizedEntries()
        {
            Seed("bannedlist.txt", "PlayFab_1A2B", SteamA);

            var result = Reconcile(new(), externalWins: true);

            Assert.Single(result.Overrides);
        }
    }
}
