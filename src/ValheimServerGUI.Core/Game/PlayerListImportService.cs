using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ValheimServerGUI.Game
{
    /// <summary>
    /// Pure logic that reconciles the on-disk <c>adminlist.txt</c> / <c>bannedlist.txt</c> /
    /// <c>permittedlist.txt</c> files with a profile's player roles. It does no UI and no file writing beyond
    /// reading the lists (via <see cref="IPlayerAccessListService"/>); the caller decides what to apply and
    /// when. Two directions:
    /// <list type="bullet">
    /// <item><see cref="BuildImport"/> — the <b>wholesale</b> file→roles adoption (ad-hoc button, first-launch
    /// auto-import). It replaces the role map with exactly what the files describe, unsetting anyone not in the
    /// files.</item>
    /// <item><see cref="CheckConflicts"/> — the <b>one-directional</b> start-time safety check when roles
    /// already exist. It only <i>adds</i> missing roles the files require and reports disagreements; it never
    /// unsets.</item>
    /// </list>
    /// </summary>
    public interface IPlayerListImportService
    {
        /// <summary>True if the given list file has at least one content (non-comment) entry.</summary>
        bool HasEntries(string saveDataFolder, PlayerAccessList list);

        /// <summary>Builds the wholesale file→roles import plan (see the type remarks). Never mutates state.</summary>
        ImportPlan BuildImport(
            string saveDataFolder,
            IReadOnlyDictionary<string, PlayerRoleEntry> currentRoles,
            bool currentFlag);

        /// <summary>Builds the start-time conflict report (see the type remarks). Never mutates state.</summary>
        ConflictReport CheckConflicts(
            string saveDataFolder,
            IReadOnlyDictionary<string, PlayerRoleEntry> currentRoles,
            bool currentFlag);
    }

    /// <summary>The result of a wholesale <see cref="IPlayerListImportService.BuildImport"/>.</summary>
    /// <param name="Success">False when a token could not be resolved (see <paramref name="FailureReason"/>).</param>
    /// <param name="FailureReason">Human-readable failure detail for the log; null on success.</param>
    /// <param name="AnyFilesPresent">False when none of the three list files exist on disk.</param>
    /// <param name="UpdateCount">Count of real changes: role adds + changes + removes + a permitted-list flag flip.</param>
    /// <param name="Roles">The full role map the import would apply (file-derived; keyed by <c>Platform:PlayerId</c>).</param>
    /// <param name="UsePermittedList">The permitted-list flag derived from the files (permittedlist non-empty).</param>
    /// <param name="NewPlayers">Players not previously in the role map, as assignments for name lookups.</param>
    public record ImportPlan(
        bool Success,
        string? FailureReason,
        bool AnyFilesPresent,
        int UpdateCount,
        IReadOnlyDictionary<string, PlayerRoleEntry> Roles,
        bool UsePermittedList,
        IReadOnlyList<PlayerRoleAssignment> NewPlayers);

    /// <summary>A missing role the files require, ready to fold into the profile's role map.</summary>
    /// <param name="Key">The role-map key (<c>Platform:PlayerId</c>).</param>
    /// <param name="Entry">The role + raw platform token to store.</param>
    public record RoleAddition(string Key, PlayerRoleEntry Entry);

    /// <summary>The result of a start-time <see cref="IPlayerListImportService.CheckConflicts"/>.</summary>
    /// <param name="Success">False when a token could not be resolved (see <paramref name="FailureReason"/>).</param>
    /// <param name="FailureReason">Human-readable failure detail for the log; null on success.</param>
    /// <param name="ConflictCount">Count of players whose file membership disagrees with their stored role.</param>
    /// <param name="Additions">Missing roles the files require (never unsets); fold into the profile.</param>
    /// <param name="NewPlayers">The additions as assignments, for name lookups.</param>
    /// <param name="ConflictingFiles">Which files contained at least one conflict (drives per-file backup).</param>
    /// <param name="LogLines">Per-conflict detail lines for the application log.</param>
    public record ConflictReport(
        bool Success,
        string? FailureReason,
        int ConflictCount,
        IReadOnlyList<RoleAddition> Additions,
        IReadOnlyList<PlayerRoleAssignment> NewPlayers,
        IReadOnlyList<PlayerAccessList> ConflictingFiles,
        IReadOnlyList<string> LogLines);

    public class PlayerListImportService : IPlayerListImportService
    {
        private readonly IPlayerAccessListService _accessLists;

        public PlayerListImportService(IPlayerAccessListService accessLists)
        {
            _accessLists = accessLists;
        }

        // The three lists in banned > admin > permitted precedence: when one player resolves into several
        // files, the first match here wins (a ban is respected; an admin naturally also sits in permittedlist
        // in permitted mode). Iterating in this order also lets an admin adopted from adminlist satisfy a
        // later permittedlist "permitted-or-admin" requirement at start.
        private static readonly (PlayerAccessList list, PlayerRole role)[] Precedence =
        {
            (PlayerAccessList.Banned, PlayerRole.Banned),
            (PlayerAccessList.Admin, PlayerRole.Admin),
            (PlayerAccessList.Permitted, PlayerRole.Permitted),
        };

        public bool HasEntries(string saveDataFolder, PlayerAccessList list)
            => _accessLists.ReadEntries(saveDataFolder, list).Count > 0;

        public ImportPlan BuildImport(
            string saveDataFolder,
            IReadOnlyDictionary<string, PlayerRoleEntry> currentRoles,
            bool currentFlag)
        {
            var dir = new DirectoryInfo(saveDataFolder);
            var anyFiles = dir.GetAdminListFile().Exists
                || dir.GetBannedListFile().Exists
                || dir.GetPermittedListFile().Exists;

            if (!anyFiles)
            {
                return new ImportPlan(
                    Success: true, FailureReason: null, AnyFilesPresent: false, UpdateCount: 0,
                    Roles: new Dictionary<string, PlayerRoleEntry>(), UsePermittedList: currentFlag,
                    NewPlayers: Array.Empty<PlayerRoleAssignment>());
            }

            // Resolve every token in every list up-front so any failure fails the whole import.
            var permittedHasEntries = _accessLists.ReadEntries(saveDataFolder, PlayerAccessList.Permitted).Count > 0;
            var target = new Dictionary<string, PlayerRoleEntry>();
            var resolvedById = new Dictionary<string, ResolvedToken>();

            foreach (var (list, role) in Precedence)
            {
                foreach (var token in _accessLists.ReadEntries(saveDataFolder, list))
                {
                    if (!PlayerListToken.TryResolve(token, out var platform, out var platformRaw, out var playerId))
                    {
                        return Failure($"Could not resolve token '{token}' from {FileName(list)}.");
                    }

                    var key = $"{platform}:{playerId}";
                    resolvedById[key] = new ResolvedToken(platform, platformRaw, playerId);

                    // Precedence order guarantees banned wins over admin wins over permitted: only take a role
                    // for a player we have not already assigned a higher-precedence one.
                    if (!target.ContainsKey(key))
                        target[key] = new PlayerRoleEntry(role, platformRaw);
                }
            }

            var usePermittedList = permittedHasEntries;

            // Diff the file-derived target against the current config to count real changes.
            var updateCount = 0;
            foreach (var (key, entry) in target)
            {
                if (!currentRoles.TryGetValue(key, out var existing) || existing != entry) updateCount++;
            }
            // Literal unset: any current player not present in the files is a removal.
            updateCount += currentRoles.Keys.Count(k => !target.ContainsKey(k));
            if (usePermittedList != currentFlag) updateCount++;

            var newPlayers = target.Keys
                .Where(k => !currentRoles.ContainsKey(k))
                .Select(k => Assignment(resolvedById[k], target[k].Role))
                .ToList();

            return new ImportPlan(
                Success: true, FailureReason: null, AnyFilesPresent: true, UpdateCount: updateCount,
                Roles: target, UsePermittedList: usePermittedList, NewPlayers: newPlayers);

            static ImportPlan Failure(string reason) => new(
                Success: false, FailureReason: reason, AnyFilesPresent: true, UpdateCount: 0,
                Roles: new Dictionary<string, PlayerRoleEntry>(), UsePermittedList: false,
                NewPlayers: Array.Empty<PlayerRoleAssignment>());
        }

        public ConflictReport CheckConflicts(
            string saveDataFolder,
            IReadOnlyDictionary<string, PlayerRoleEntry> currentRoles,
            bool currentFlag)
        {
            var additions = new Dictionary<string, RoleAddition>();
            var resolvedById = new Dictionary<string, ResolvedToken>();
            var conflictingFiles = new List<PlayerAccessList>();
            var logLines = new List<string>();
            var conflicts = new HashSet<string>();

            // Process in banned > admin > permitted order so an admin adopted from adminlist satisfies a later
            // permittedlist requirement (which accepts admin-or-permitted) rather than double-adding.
            foreach (var (list, expected) in Precedence)
            {
                foreach (var token in _accessLists.ReadEntries(saveDataFolder, list))
                {
                    if (!PlayerListToken.TryResolve(token, out var platform, out var platformRaw, out var playerId))
                    {
                        return new ConflictReport(
                            Success: false, FailureReason: $"Could not resolve token '{token}' from {FileName(list)}.",
                            ConflictCount: 0, Additions: Array.Empty<RoleAddition>(),
                            NewPlayers: Array.Empty<PlayerRoleAssignment>(),
                            ConflictingFiles: Array.Empty<PlayerAccessList>(), LogLines: Array.Empty<string>());
                    }

                    var key = $"{platform}:{playerId}";
                    resolvedById[key] = new ResolvedToken(platform, platformRaw, playerId);

                    // Effective role = a pending addition (this pass) overlaid on the stored config.
                    PlayerRole? effective = additions.TryGetValue(key, out var pending)
                        ? pending.Entry.Role
                        : currentRoles.TryGetValue(key, out var existing) ? existing.Role : null;

                    if (effective is null)
                    {
                        // Missing from config → adopt the role the file requires.
                        additions[key] = new RoleAddition(key, new PlayerRoleEntry(expected, platformRaw));
                    }
                    else if (!Satisfies(effective.Value, list))
                    {
                        // Present but with a role the file disagrees with → conflict.
                        if (conflicts.Add(key))
                        {
                            logLines.Add(
                                $"Role conflict: {FileName(list)} lists {platform}_{playerId} " +
                                $"(expected {expected}), but the profile has {effective.Value}.");
                        }
                        if (!conflictingFiles.Contains(list)) conflictingFiles.Add(list);
                    }
                }
            }

            var additionList = additions.Values.ToList();
            var newPlayers = additionList
                .Select(a => Assignment(resolvedById[a.Key], a.Entry.Role))
                .ToList();

            return new ConflictReport(
                Success: true, FailureReason: null, ConflictCount: conflicts.Count,
                Additions: additionList, NewPlayers: newPlayers,
                ConflictingFiles: conflictingFiles, LogLines: logLines);
        }

        #region Non-public

        private sealed record ResolvedToken(string Platform, string PlatformRaw, string PlayerId);

        // Whether a stored role satisfies a file's membership requirement. Admin and Banned require the exact
        // role; the permitted list accepts a permitted OR an admin player (an admin must be permitted to join
        // in permitted-list mode).
        private static bool Satisfies(PlayerRole role, PlayerAccessList list) => list switch
        {
            PlayerAccessList.Admin => role == PlayerRole.Admin,
            PlayerAccessList.Banned => role == PlayerRole.Banned,
            PlayerAccessList.Permitted => role is PlayerRole.Permitted or PlayerRole.Admin,
            _ => false,
        };

        private static PlayerRoleAssignment Assignment(ResolvedToken t, PlayerRole role)
            => new(t.Platform, t.PlatformRaw, t.PlayerId, role);

        private static string FileName(PlayerAccessList list) => list switch
        {
            PlayerAccessList.Admin => "adminlist.txt",
            PlayerAccessList.Banned => "bannedlist.txt",
            PlayerAccessList.Permitted => "permittedlist.txt",
            _ => list.ToString(),
        };

        #endregion
    }
}
