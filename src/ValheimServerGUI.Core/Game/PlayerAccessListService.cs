using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ValheimServerGUI.Tools.Models;

namespace ValheimServerGUI.Game
{
    /// <summary>The three player-gating files Valheim reads from the save-data root.</summary>
    public enum PlayerAccessList
    {
        Admin,
        Banned,
        Permitted,
    }

    /// <summary>
    /// Profile-scoped read/write of the <c>adminlist.txt</c> / <c>bannedlist.txt</c> / <c>permittedlist.txt</c>
    /// files that live in the save-data root (the server's <c>-savedir</c>). Entries are compared by the player
    /// they name (<see cref="PlayerListToken"/>), so the bare <c>&lt;steam64&gt;</c> and prefixed
    /// <c>Steam_&lt;steam64&gt;</c> forms are the same player. Every operation takes the savedir explicitly so the
    /// service holds no global state and is fully testable.
    /// </summary>
    public interface IPlayerAccessListService
    {
        /// <summary>
        /// The content (ID) lines of a list file -- comment (<c>//</c>) and blank lines excluded. Empty when
        /// the file is missing.
        /// </summary>
        IReadOnlyList<string> ReadEntries(string saveDataFolder, PlayerAccessList list);

        /// <summary>
        /// Rewrites all three list files in <paramref name="saveDataFolder"/> from the given player roles and the
        /// <paramref name="usePermittedList"/> flag, per <see cref="PlayerAccessListRules"/>, without ever losing an
        /// entry VSG did not write itself:
        /// <list type="bullet">
        /// <item>Comment lines and entries VSG cannot resolve to a player are carried through verbatim.</item>
        /// <item>A file that would lose an entry absent from <paramref name="baseline"/> (VSG's previous write) is
        /// copied to a backup first. With no baseline, every entry counts as external.</item>
        /// <item>If any needed backup cannot be made, nothing is written and <see cref="PlayerListBackupException"/>
        /// is thrown.</item>
        /// </list>
        /// Returns what was written (the next baseline) and the backups made.
        /// </summary>
        PlayerListWriteResult WriteLists(
            string saveDataFolder,
            IEnumerable<(PlayerInfo player, PlayerRole role)> roles,
            bool usePermittedList,
            PlayerListBaseline? baseline);
    }

    /// <summary>The outcome of <see cref="IPlayerAccessListService.WriteLists"/>.</summary>
    /// <param name="Written">The entries now in each file — the baseline for the next write.</param>
    /// <param name="Backups">Backup copies made because entries VSG did not write were about to be dropped.</param>
    public record PlayerListWriteResult(PlayerListBaseline Written, IReadOnlyList<FileInfo> Backups);

    /// <summary>A list file had to be backed up before being rewritten, and the backup failed; nothing was written.</summary>
    public class PlayerListBackupException : IOException
    {
        public PlayerListBackupException(string filePath)
            : base($"Could not back up {filePath} before rewriting it; no list files were changed.")
        {
            FilePath = filePath;
        }

        public string FilePath { get; }
    }

    public class PlayerAccessListService : IPlayerAccessListService
    {
        private const string CommentPrefix = "//";

        // Exact headers the game writes (captured from the real binary's generated files). Admin/Banned use
        // two spaces before "ONE"; Permitted uses one. Only used when VSG creates a file before the server
        // has ever run -- an existing file's header is preserved verbatim.
        private static readonly IReadOnlyDictionary<PlayerAccessList, string> DefaultHeaders =
            new Dictionary<PlayerAccessList, string>
            {
                [PlayerAccessList.Admin] = "// List admin players ID  ONE per line",
                [PlayerAccessList.Banned] = "// List banned players ID  ONE per line",
                [PlayerAccessList.Permitted] = "// List permitted players ID ONE per line",
            };

        public IReadOnlyList<string> ReadEntries(string saveDataFolder, PlayerAccessList list)
        {
            var file = ResolveFile(saveDataFolder, list);
            if (!file.Exists) return Array.Empty<string>();

            return File.ReadAllLines(file.FullName)
                .Select(line => line.Trim())
                .Where(IsEntryLine)
                .ToList();
        }

        public PlayerListWriteResult WriteLists(
            string saveDataFolder,
            IEnumerable<(PlayerInfo player, PlayerRole role)> roles,
            bool usePermittedList,
            PlayerListBaseline? baseline)
        {
            var assignments = roles.ToList();

            // Plan every file before touching any, so a failed backup leaves all three exactly as they were.
            var plans = AllLists.Select(list => PlanFile(saveDataFolder, list, assignments, usePermittedList, baseline)).ToList();

            var backups = new List<FileInfo>();
            foreach (var plan in plans.Where(p => p.NeedsBackup))
            {
                var backup = ValheimPathExtensions.CopyListFileToBackup(plan.File)
                    ?? throw new PlayerListBackupException(plan.File.FullName);
                backups.Add(backup);
            }

            foreach (var plan in plans.Where(p => p.Changed)) AtomicWrite(plan.File, plan.Lines);

            var written = plans.ToDictionary(p => p.List, p => (IReadOnlyList<string>)p.Entries);
            return new PlayerListWriteResult(new PlayerListBaseline(written), backups);
        }

        private sealed record FilePlan(
            PlayerAccessList List, FileInfo File, List<string> Lines, List<string> Entries, bool NeedsBackup, bool Changed);

        private static FilePlan PlanFile(
            string saveDataFolder,
            PlayerAccessList list,
            IReadOnlyList<(PlayerInfo player, PlayerRole role)> assignments,
            bool usePermittedList,
            PlayerListBaseline? baseline)
        {
            var file = ResolveFile(saveDataFolder, list);
            var existing = file.Exists ? File.ReadAllLines(file.FullName) : Array.Empty<string>();
            var existingEntries = existing.Select(l => l.Trim()).Where(IsEntryLine).ToList();

            // Canonical tokens of every player whose role routes into this file for the current mode. A HashSet
            // dedupes players who happen to share a token (canonical form is deterministic).
            var tokens = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var (player, role) in assignments)
            {
                if (string.IsNullOrWhiteSpace(player.PlayerId)) continue;
                if (!PlayerAccessListRules.TargetLists(role, usePermittedList).Contains(list)) continue;

                var token = CanonicalEntry(player);
                if (seen.Add(token)) tokens.Add(token);
            }

            // Lines VSG doesn't manage are kept verbatim: comments (or the game's header for a new file), and entries
            // that don't resolve to a player VSG understands.
            var comments = existing.Where(l => l.Trim().StartsWith(CommentPrefix, StringComparison.Ordinal)).ToList();
            if (comments.Count == 0) comments.Add(DefaultHeaders[list]);
            var unrecognized = existingEntries.Where(e => Identity(e) is null).Distinct(StringComparer.Ordinal).ToList();

            var entries = unrecognized.Concat(tokens).ToList();
            var lines = comments.Concat(entries).ToList();

            // An existing entry the new content drops, which VSG didn't write last time, is someone else's: back the
            // file up before it goes. (Bare vs prefixed Steam forms are the same player, so that's not a drop.)
            var keptIds = tokens.Select(Identity).ToHashSet();
            var baselineIds = baseline?.For(list).Select(Identity).ToHashSet();
            var needsBackup = existingEntries
                .Select(Identity)
                .Any(id => id is not null && !keptIds.Contains(id) && (baselineIds is null || !baselineIds.Contains(id)));

            var changed = !file.Exists || !existing.SequenceEqual(lines);
            return new FilePlan(list, file, lines, entries, needsBackup, changed);
        }

        private static readonly PlayerAccessList[] AllLists =
            { PlayerAccessList.Admin, PlayerAccessList.Banned, PlayerAccessList.Permitted };

        /// <summary>The player an entry names (<c>Platform:PlayerId</c>), or null when VSG can't resolve it.</summary>
        private static string? Identity(string entry)
            => PlayerListToken.TryResolve(entry, out var platform, out _, out var playerId) ? $"{platform}:{playerId}" : null;

        #region Non-public

        private static FileInfo ResolveFile(string saveDataFolder, PlayerAccessList list)
        {
            var dir = new DirectoryInfo(saveDataFolder);
            return list switch
            {
                PlayerAccessList.Admin => dir.GetAdminListFile(),
                PlayerAccessList.Banned => dir.GetBannedListFile(),
                PlayerAccessList.Permitted => dir.GetPermittedListFile(),
                _ => throw new ArgumentOutOfRangeException(nameof(list), list, null),
            };
        }

        /// <summary>A content line: non-blank and not a comment.</summary>
        private static bool IsEntryLine(string trimmed)
            => trimmed.Length > 0 && !trimmed.StartsWith(CommentPrefix, StringComparison.Ordinal);

        private static bool IsSteam(PlayerInfo player)
            => string.Equals(player.Platform, PlayerPlatforms.Steam, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// The token a non-Steam entry is written with: the raw log token if we captured it, else the
        /// normalized platform name. Steam is always written canonically as "Steam".
        /// </summary>
        private static string WriteToken(PlayerInfo player)
            => string.IsNullOrWhiteSpace(player.PlatformRaw) ? player.Platform ?? string.Empty : player.PlatformRaw;

        private static string CanonicalEntry(PlayerInfo player)
            => IsSteam(player)
                ? $"{PlayerPlatforms.Steam}_{player.PlayerId}"
                : $"{WriteToken(player)}_{player.PlayerId}";

        /// <summary>
        /// Writes the file via a temp file + rename so a reader (the running game re-reads every ~5s) never
        /// observes a half-written file. Uses '\n' line endings, which the game accepts on every OS.
        /// </summary>
        private static void AtomicWrite(FileInfo file, IEnumerable<string> lines)
        {
            var dir = file.Directory;
            if (dir is not null && !dir.Exists) dir.Create();

            var content = string.Join('\n', lines) + '\n';
            var temp = file.FullName + ".tmp";
            File.WriteAllText(temp, content);
            File.Move(temp, file.FullName, overwrite: true);
        }

        #endregion
    }
}
