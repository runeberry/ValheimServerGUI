using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ValheimServerGUI.Game
{
    /// <summary>
    /// Pure logic that reconciles the on-disk <c>adminlist.txt</c> / <c>bannedlist.txt</c> / <c>permittedlist.txt</c>
    /// files with a profile's player roles. A profile stores role <b>overrides</b> layered over the app-global player
    /// <b>defaults</b> (see <see cref="PlayerRoleResolver"/>). It does no UI and no writing; the caller decides what to
    /// apply and when. Entries VSG can't resolve to a player are skipped (and reported), never fatal. Two directions:
    /// <list type="bullet">
    /// <item><see cref="BuildImport"/> — the <b>wholesale</b> file→roles adoption (ad-hoc button, first-launch
    /// auto-import). It rebuilds the override map so the effective roles match the files.</item>
    /// <item><see cref="Reconcile"/> — adopts only what changed on disk since VSG's own last write
    /// (<see cref="PlayerListBaseline"/>): the game's in-game ban/permit commands, or a hand edit.</item>
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
            bool currentFlag,
            IReadOnlyDictionary<string, PlayerDefaultEntry>? defaults = null);

        /// <summary>
        /// Adopts the changes made to the files since VSG last wrote them (see <see cref="ReconcileResult"/>),
        /// per the folder's <see cref="PlayerListBaseline"/>. With no baseline (VSG has no record of writing here),
        /// every entry counts as added. Never mutates state.
        /// </summary>
        /// <param name="externalWins">When an added entry disagrees with the player's effective role: true adopts the
        /// file's role (a change made during play wins); false keeps the profile and reports a conflict.</param>
        ReconcileResult Reconcile(
            string saveDataFolder,
            IReadOnlyDictionary<string, PlayerRoleEntry> overrides,
            IReadOnlyDictionary<string, PlayerDefaultEntry> defaults,
            bool externalWins);
    }

    /// <summary>The result of a wholesale <see cref="IPlayerListImportService.BuildImport"/>.</summary>
    /// <param name="AnyFilesPresent">False when none of the three list files exist on disk.</param>
    /// <param name="UpdateCount">Count of real changes: players whose effective role changes + a permitted-list flag flip.</param>
    /// <param name="Roles">The full override map the import would apply (keyed by <c>Platform:PlayerId</c>).</param>
    /// <param name="UsePermittedList">The permitted-list flag derived from the files (permittedlist non-empty).</param>
    /// <param name="NewPlayers">Players with neither an override nor a default before, as assignments for name lookups.</param>
    /// <param name="Unrecognized">Entries skipped because they don't resolve to a player (they stay in the files).</param>
    public record ImportPlan(
        bool AnyFilesPresent,
        int UpdateCount,
        IReadOnlyDictionary<string, PlayerRoleEntry> Roles,
        bool UsePermittedList,
        IReadOnlyList<PlayerRoleAssignment> NewPlayers,
        IReadOnlyList<string> Unrecognized);

    /// <summary>An entry added to a file since VSG's last write that disagrees with the player's effective role.</summary>
    public record ListConflict(string Key, PlayerAccessList List, PlayerRole FileRole, PlayerRole ProfileRole);

    /// <summary>The result of <see cref="IPlayerListImportService.Reconcile"/>.</summary>
    /// <param name="Overrides">The profile's override map with the changes applied.</param>
    /// <param name="Changed">True when <paramref name="Overrides"/> differs from the input.</param>
    /// <param name="Conflicts">Added entries that disagreed with the profile (adopted only when external changes win).</param>
    /// <param name="NewPlayers">Players adopted from the files who had neither an override nor a default, for name lookups.</param>
    /// <param name="LogLines">One line per adoption, conflict, and kept role, for the application log.</param>
    public record ReconcileResult(
        IReadOnlyDictionary<string, PlayerRoleEntry> Overrides,
        bool Changed,
        IReadOnlyList<ListConflict> Conflicts,
        IReadOnlyList<PlayerRoleAssignment> NewPlayers,
        IReadOnlyList<string> LogLines);

    public class PlayerListImportService : IPlayerListImportService
    {
        private readonly IPlayerAccessListService _accessLists;
        private readonly IPlayerListBaselineStore _baselines;

        public PlayerListImportService(IPlayerAccessListService accessLists, IPlayerListBaselineStore baselines)
        {
            _accessLists = accessLists;
            _baselines = baselines;
        }

        // The three lists in banned > admin > permitted precedence: when one player resolves into several
        // files, the first match here wins (a ban is respected; an admin naturally also sits in permittedlist
        // in permitted mode).
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
            bool currentFlag,
            IReadOnlyDictionary<string, PlayerDefaultEntry>? defaults = null)
        {
            defaults ??= new Dictionary<string, PlayerDefaultEntry>();

            var dir = new DirectoryInfo(saveDataFolder);
            var anyFiles = dir.GetAdminListFile().Exists
                || dir.GetBannedListFile().Exists
                || dir.GetPermittedListFile().Exists;

            if (!anyFiles)
            {
                return new ImportPlan(
                    AnyFilesPresent: false, UpdateCount: 0,
                    Roles: new Dictionary<string, PlayerRoleEntry>(), UsePermittedList: currentFlag,
                    NewPlayers: Array.Empty<PlayerRoleAssignment>(), Unrecognized: Array.Empty<string>());
            }

            var permittedHasEntries = _accessLists.ReadEntries(saveDataFolder, PlayerAccessList.Permitted).Count > 0;
            var desired = new Dictionary<string, PlayerRoleEntry>();
            var resolvedById = new Dictionary<string, ResolvedToken>();
            var unrecognized = new List<string>();

            foreach (var (list, role) in Precedence)
            {
                foreach (var token in _accessLists.ReadEntries(saveDataFolder, list))
                {
                    if (!PlayerListToken.TryResolve(token, out var platform, out var platformRaw, out var playerId))
                    {
                        // Not a player VSG understands: skip it. File writes carry it through untouched.
                        unrecognized.Add($"{FileName(list)}: {token}");
                        continue;
                    }

                    var key = $"{platform}:{playerId}";
                    resolvedById[key] = new ResolvedToken(platform, platformRaw, playerId);

                    // Precedence order guarantees banned wins over admin wins over permitted: only take a role
                    // for a player we have not already assigned a higher-precedence one.
                    if (!desired.ContainsKey(key))
                        desired[key] = new PlayerRoleEntry(role, platformRaw);
                }
            }

            var usePermittedList = permittedHasEntries;

            // Rebuild the override map so every player's effective role matches the files (absent = None). A player
            // the files agree with needs no override unless one already pins them. Absent from the files means "no
            // role", which a server can't override to, so the player falls back to their default (if any).
            var overrides = new Dictionary<string, PlayerRoleEntry>();
            var updateCount = 0;
            foreach (var key in desired.Keys.Union(currentRoles.Keys).Union(defaults.Keys))
            {
                var desiredRole = desired.TryGetValue(key, out var fromFile) ? fromFile.Role : PlayerRole.None;
                var hasDefault = defaults.TryGetValue(key, out var def);
                var baseline = hasDefault ? def!.DefaultRole : PlayerRole.None;
                var hadOverride = currentRoles.TryGetValue(key, out var existing);

                var keepsOverride = desiredRole != PlayerRole.None
                    && (desiredRole != baseline || (hadOverride && hasDefault));
                if (keepsOverride)
                {
                    var platformRaw = fromFile?.PlatformRaw ?? existing?.PlatformRaw ?? def?.PlatformRaw;
                    overrides[key] = new PlayerRoleEntry(desiredRole, platformRaw);
                }

                var newEffective = keepsOverride ? desiredRole : baseline;
                if (PlayerRoleResolver.Resolve(key, currentRoles, defaults).Effective != newEffective) updateCount++;
            }
            if (usePermittedList != currentFlag) updateCount++;

            var newPlayers = desired.Keys
                .Where(k => !currentRoles.ContainsKey(k) && !defaults.ContainsKey(k))
                .Select(k => Assignment(resolvedById[k], desired[k].Role))
                .ToList();

            return new ImportPlan(
                AnyFilesPresent: true, UpdateCount: updateCount, Roles: overrides,
                UsePermittedList: usePermittedList, NewPlayers: newPlayers, Unrecognized: unrecognized);
        }

        public ReconcileResult Reconcile(
            string saveDataFolder,
            IReadOnlyDictionary<string, PlayerRoleEntry> overrides,
            IReadOnlyDictionary<string, PlayerDefaultEntry> defaults,
            bool externalWins)
        {
            var baseline = _baselines.Get(saveDataFolder);
            var result = new Dictionary<string, PlayerRoleEntry>(overrides);
            var conflicts = new List<ListConflict>();
            var newPlayers = new List<PlayerRoleAssignment>();
            var log = new List<string>();

            // Per list: the players on disk now, and the players VSG wrote there last time.
            var current = new Dictionary<PlayerAccessList, Dictionary<string, ResolvedToken>>();
            var previous = new Dictionary<PlayerAccessList, HashSet<string>>();
            foreach (var (list, _) in Precedence)
            {
                current[list] = Resolve(_accessLists.ReadEntries(saveDataFolder, list));
                previous[list] = Resolve(baseline?.For(list) ?? Array.Empty<string>()).Keys.ToHashSet();
            }

            // Additions: on disk but not written by VSG. Banned > admin > permitted when one player was added to
            // several lists at once.
            var added = new Dictionary<string, (PlayerAccessList List, PlayerRole Role, ResolvedToken Token)>();
            foreach (var (list, role) in Precedence)
            {
                foreach (var (key, token) in current[list])
                {
                    if (!previous[list].Contains(key) && !added.ContainsKey(key)) added[key] = (list, role, token);
                }
            }

            foreach (var (key, (list, role, token)) in added)
            {
                var resolved = PlayerRoleResolver.Resolve(key, result, defaults);
                if (Satisfies(resolved.Effective, list)) continue;

                if (resolved.Effective != PlayerRole.None)
                {
                    conflicts.Add(new ListConflict(key, list, role, resolved.Effective));
                    if (!externalWins)
                    {
                        log.Add($"Role conflict: {FileName(list)} lists {key} (as {role}), but the profile has {resolved.Effective}.");
                        continue;
                    }
                }

                result[key] = new PlayerRoleEntry(role, token.PlatformRaw);
                log.Add(resolved.Effective == PlayerRole.None
                    ? $"Adopted {key} as {role} from {FileName(list)}."
                    : $"Adopted {key} as {role} from {FileName(list)} (was {resolved.Effective}).");
                if (!resolved.HasOverride && !resolved.HasDefault) newPlayers.Add(Assignment(token, role));
            }

            // Removals: written by VSG last time but gone from disk now (e.g. an in-game unban). Undo the server's own
            // override for that role; a global default is left alone.
            foreach (var (list, role) in Precedence)
            {
                foreach (var key in previous[list].Where(k => !current[list].ContainsKey(k) && !added.ContainsKey(k)))
                {
                    var resolved = PlayerRoleResolver.Resolve(key, result, defaults);
                    if (resolved.Effective != role) continue; // the role it was written for no longer applies anyway

                    if (resolved.HasOverride && result[key].Role == role)
                    {
                        result.Remove(key);
                        var fallback = PlayerRoleResolver.Resolve(key, result, defaults).Effective;
                        log.Add(fallback == role
                            ? $"{key} was removed from {FileName(list)}; removed the server's {role} role, but the player's global default is also {role}."
                            : $"{key} was removed from {FileName(list)}; removed the server's {role} role.");
                    }
                    else
                    {
                        log.Add($"{key} was removed from {FileName(list)}, but {role} is the player's global default; kept.");
                    }
                }
            }

            var changed = result.Count != overrides.Count
                || result.Any(kvp => !overrides.TryGetValue(kvp.Key, out var before) || before != kvp.Value);
            return new ReconcileResult(result, changed, conflicts, newPlayers, log);
        }

        // Resolvable entries keyed by Platform:PlayerId (bare and prefixed Steam forms collapse to one player).
        private static Dictionary<string, ResolvedToken> Resolve(IEnumerable<string> entries)
        {
            var resolved = new Dictionary<string, ResolvedToken>();
            foreach (var entry in entries)
            {
                if (PlayerListToken.TryResolve(entry, out var platform, out var platformRaw, out var playerId))
                    resolved.TryAdd($"{platform}:{playerId}", new ResolvedToken(platform, platformRaw, playerId));
            }
            return resolved;
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
