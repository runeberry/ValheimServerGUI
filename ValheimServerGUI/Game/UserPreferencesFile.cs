using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ValheimServerGUI.Game
{
    /// <summary>
    /// Represents the deserialized user preferences file, and provides the model as it's
    /// written to / read from disk.
    /// </summary>
    public class UserPreferencesFile
    {
        [JsonProperty("valheimGamePath")]
        public string ValheimGamePath { get; set; }

        [JsonProperty("valheimServerPath")]
        public string ValheimServerPath { get; set; }

        [JsonProperty("valheimSaveDataFolder")]
        public string ValheimSaveDataFolder { get; set; }

        [JsonProperty("startWithWindows"), Obsolete("Moved to server preferences", true)]
        public bool? StartWithWindows { get; set; }

        [JsonProperty("startServerAutomatically"), Obsolete("Moved to server preferences", true)]
        public bool? StartServerAutomatically { get; set; }

        [JsonProperty("startMinimized"), Obsolete("Moved to server preferences", true)]
        public bool? StartMinimized { get; set; }

        [JsonProperty("checkServerRunning"), Obsolete("Removed with multi-server support", true)]
        public bool? CheckServerRunning { get; set; }

        [JsonProperty("checkForUpdates")]
        public bool? CheckForUpdates { get; set; }

        [JsonProperty("saveProfileOnStart")]
        public bool? SaveProfileOnStart { get; set; }

        [JsonProperty("servers")]
        public List<ServerPreferencesFile> Servers { get; set; }
    }
}
