using Newtonsoft.Json;
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

        [JsonProperty("startWithWindows")]
        public bool? StartWithWindows { get; set; }

        [JsonProperty("startServerAutomatically")]
        public bool? StartServerAutomatically { get; set; }

        [JsonProperty("startMinimized")]
        public bool? StartMinimized { get; set; }

        [JsonProperty("checkServerRunning")]
        public bool? CheckServerRunning { get; set; }

        [JsonProperty("checkForUpdates")]
        public bool? CheckForUpdates { get; set; }

        [JsonProperty("servers")]
        public List<ServerPreferencesFile> Servers { get; set; }
    }
}
