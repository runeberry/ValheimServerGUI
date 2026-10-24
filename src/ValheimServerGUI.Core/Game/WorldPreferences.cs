using System;
using System.Collections.Generic;
using System.Linq;

namespace ValheimServerGUI.Game
{
    public class WorldPreferences
    {
        public string? WorldName { get; set; }

        public DateTime LastSaved { get; set; } = DateTime.UnixEpoch;

        public Dictionary<string, string> Modifiers { get; set; } = new();

        public HashSet<string> Keys { get; set; } = new();

        public static WorldPreferences FromFile(WorldPreferencesFile? file)
        {
            var prefs = new WorldPreferences();

            if (file == null) return prefs;

            prefs.WorldName = file.WorldName;
            prefs.LastSaved = file.LastSaved ?? prefs.LastSaved;
            // A JSON file with null "modifiers"/"keys" (absent or explicitly null) must not NPE.
            prefs.Modifiers = file.Modifiers ?? prefs.Modifiers;
            prefs.Keys = file.Keys?.ToHashSet() ?? prefs.Keys;

            // Older files store a chosen preset by name instead of its values; expand it so the fields hold the truth.
            // Keys saved alongside it are kept.
            if (WorldGenPresets.Find(file.Preset) is { } preset)
            {
                foreach (var (modifier, value) in preset.Settings.Modifiers) prefs.Modifiers.TryAdd(modifier, value);
                prefs.Keys.UnionWith(preset.Settings.Keys);
            }

            return prefs;
        }

        /// <summary>The world settings these preferences launch the server with.</summary>
        public WorldGenSettings ToSettings() => new(Modifiers, Keys);

        public WorldPreferencesFile ToFile()
        {
            var file = new WorldPreferencesFile()
            {
                WorldName = WorldName,
                LastSaved = LastSaved,
                Modifiers = Modifiers,
                Keys = Keys.ToList(),
            };

            return file;
        }
    }
}
