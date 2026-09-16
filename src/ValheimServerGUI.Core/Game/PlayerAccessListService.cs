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
    /// files that live in the save-data root (the server's <c>-savedir</c>). Matching mirrors the game's
    /// <c>ZNet.ListContainsId</c>: Steam matches either the bare <c>&lt;steam64&gt;</c> or the prefixed
    /// <c>Steam_&lt;steam64&gt;</c> form; non-Steam matches only <c>&lt;Platform&gt;_&lt;id&gt;</c>,
    /// case-sensitive against the raw platform token. Every operation takes the savedir explicitly so the
    /// service holds no global state and is fully testable.
    /// </summary>
    public interface IPlayerAccessListService
    {
        /// <summary>
        /// The content (ID) lines of a list file -- comment (<c>//</c>) and blank lines excluded. Empty when
        /// the file is missing. Read once per list and passed to <see cref="Contains"/> to compute row
        /// membership in bulk without re-reading per player.
        /// </summary>
        IReadOnlyList<string> ReadEntries(string saveDataFolder, PlayerAccessList list);

        /// <summary>True if <paramref name="player"/> is present among <paramref name="entries"/> (game matching).</summary>
        bool Contains(IReadOnlyList<string> entries, PlayerInfo player);

        /// <summary>Convenience: reads the file and tests membership in one call.</summary>
        bool Contains(string saveDataFolder, PlayerAccessList list, PlayerInfo player);

        /// <summary>
        /// Adds the player to the list (Steam written canonically as <c>Steam_&lt;id&gt;</c>, non-Steam as
        /// <c>&lt;RawPlatform&gt;_&lt;id&gt;</c>), creating the file with the game's header if needed. Returns
        /// true if the file changed; false if the player was already present in any recognized form.
        /// </summary>
        bool Add(string saveDataFolder, PlayerAccessList list, PlayerInfo player);

        /// <summary>
        /// Removes every entry matching the player (both bare and prefixed Steam forms). Returns true if the
        /// file changed; false if nothing matched (or the file did not exist).
        /// </summary>
        bool Remove(string saveDataFolder, PlayerAccessList list, PlayerInfo player);
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

        public bool Contains(IReadOnlyList<string> entries, PlayerInfo player)
            => entries.Any(entry => Matches(entry, player));

        public bool Contains(string saveDataFolder, PlayerAccessList list, PlayerInfo player)
            => Contains(ReadEntries(saveDataFolder, list), player);

        public bool Add(string saveDataFolder, PlayerAccessList list, PlayerInfo player)
        {
            if (string.IsNullOrWhiteSpace(player.PlayerId)) return false;

            var file = ResolveFile(saveDataFolder, list);
            var lines = file.Exists
                ? File.ReadAllLines(file.FullName).ToList()
                : new List<string> { DefaultHeaders[list] };

            // Already present in any recognized form (bare or prefixed for Steam) -> no change.
            if (lines.Any(line => Matches(line.Trim(), player))) return false;

            lines.Add(CanonicalEntry(player));
            AtomicWrite(file, lines);
            return true;
        }

        public bool Remove(string saveDataFolder, PlayerAccessList list, PlayerInfo player)
        {
            if (string.IsNullOrWhiteSpace(player.PlayerId)) return false;

            var file = ResolveFile(saveDataFolder, list);
            if (!file.Exists) return false;

            var lines = File.ReadAllLines(file.FullName).ToList();
            // Drop every matching entry (removes both the bare and prefixed Steam forms if both exist).
            var removed = lines.RemoveAll(line => Matches(line.Trim(), player));
            if (removed == 0) return false;

            AtomicWrite(file, lines);
            return true;
        }

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

        /// <summary>Game matching semantics (<c>ZNet.ListContainsId</c>).</summary>
        private static bool Matches(string entry, PlayerInfo player)
        {
            var id = player.PlayerId;
            if (string.IsNullOrWhiteSpace(id)) return false;

            if (IsSteam(player))
            {
                // Steam: bare <id> (exact) or Steam_<id> (prefix, casing-lenient on the token).
                return entry == id
                    || string.Equals(entry, $"{PlayerPlatforms.Steam}_{id}", StringComparison.OrdinalIgnoreCase);
            }

            // Non-Steam: exact, case-sensitive <Platform>_<id>, matching what we would write.
            return entry == $"{WriteToken(player)}_{id}";
        }

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
