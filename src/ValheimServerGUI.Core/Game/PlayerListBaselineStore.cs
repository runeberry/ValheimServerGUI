using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace ValheimServerGUI.Game
{
    /// <summary>
    /// The entries VSG last wrote to each list file of one save folder. Comparing the files on disk against it is
    /// how VSG tells its own previous writes apart from changes made by someone else (the game's in-game
    /// ban/permit commands, or a hand edit).
    /// </summary>
    public sealed record PlayerListBaseline(IReadOnlyDictionary<PlayerAccessList, IReadOnlyList<string>> Entries)
    {
        public IReadOnlyList<string> For(PlayerAccessList list)
            => Entries.TryGetValue(list, out var entries) ? entries : Array.Empty<string>();
    }

    /// <summary>Persists the <see cref="PlayerListBaseline"/> of each save folder in the app's own data folder.</summary>
    public interface IPlayerListBaselineStore
    {
        /// <summary>The last-written entries for <paramref name="saveDataFolder"/>, or null when VSG has no record of
        /// writing there (first run, a moved install, or an unreadable store) — callers then treat every entry on
        /// disk as written by someone else.</summary>
        PlayerListBaseline? Get(string saveDataFolder);

        void Save(string saveDataFolder, PlayerListBaseline baseline);
    }

    public class PlayerListBaselineStore : IPlayerListBaselineStore
    {
        private const string FileName = "list-baselines.json";

        private readonly string _filePath;
        private readonly object _lock = new();

        public PlayerListBaselineStore(IValheimPathResolver pathResolver)
        {
            _filePath = pathResolver.GetAppDataPath(FileName);
        }

        public PlayerListBaseline? Get(string saveDataFolder)
        {
            lock (_lock)
            {
                if (!ReadAll().TryGetValue(Key(saveDataFolder), out var lists)) return null;

                var entries = new Dictionary<PlayerAccessList, IReadOnlyList<string>>();
                foreach (var (name, values) in lists)
                {
                    if (Enum.TryParse<PlayerAccessList>(name, ignoreCase: true, out var list)) entries[list] = values;
                }
                return new PlayerListBaseline(entries);
            }
        }

        public void Save(string saveDataFolder, PlayerListBaseline baseline)
        {
            lock (_lock)
            {
                var all = ReadAll();
                all[Key(saveDataFolder)] = baseline.Entries.ToDictionary(
                    kvp => kvp.Key.ToString().ToLowerInvariant(), kvp => kvp.Value.ToList());

                Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
                var temp = $"{_filePath}.{Guid.NewGuid():N}.tmp"; // unique: another process may be saving too
                File.WriteAllText(temp, JsonConvert.SerializeObject(all, Formatting.Indented));
                File.Move(temp, _filePath, overwrite: true);
            }
        }

        // Save folder path → list name ("admin"/"banned"/"permitted") → entries.
        private Dictionary<string, Dictionary<string, List<string>>> ReadAll()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    return JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, List<string>>>>(
                        File.ReadAllText(_filePath)) ?? new();
                }
            }
            catch (JsonException)
            {
                // An unreadable store is the same as no record: every entry on disk counts as external.
            }
            return new();
        }

        private static string Key(string saveDataFolder)
            => Path.TrimEndingDirectorySeparator(Path.GetFullPath(saveDataFolder));
    }
}
