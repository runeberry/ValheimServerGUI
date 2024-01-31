using System;
using System.Collections.Generic;
using System.Linq;

namespace ValheimServerGUI.Game
{
    public class WorldPreferences
    {
        public string WorldName { get; set; }

        public DateTime LastSaved { get; set; } = DateTime.UnixEpoch;

        public string Preset { get; set; }

        public Dictionary<string, string> Modifiers { get; set; } = new();

        public HashSet<string> Keys { get; set; } = new();

        public static WorldPreferences FromFile(WorldPreferencesFile file)
        {
            var prefs = new WorldPreferences();

            if (file == null) return prefs;

            prefs.WorldName = file.WorldName;
            prefs.LastSaved = file.LastSaved ?? prefs.LastSaved;
            prefs.Preset = file.Preset;
            prefs.Modifiers = file.Modifiers;
            prefs.Keys = file.Keys.ToHashSet();

            return prefs;
        }

        public WorldPreferencesFile ToFile()
        {
            var file = new WorldPreferencesFile()
            {
                WorldName = WorldName,
                LastSaved = LastSaved,
                Preset = Preset,
                Modifiers = Modifiers,
                Keys = Keys.ToList(),
            };

            return file;
        }
    }
}
