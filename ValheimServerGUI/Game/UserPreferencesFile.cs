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

        public class ServerPreferencesFile
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("password")]
            public string Password { get; set; }

            [JsonProperty("world")]
            public string WorldName { get; set; }

            [JsonProperty("community")]
            public bool? CommunityServer { get; set; }

            [JsonProperty("port")]
            public int? Port { get; set; }

            [JsonProperty("crossplay")]
            public bool? Crossplay { get; set; }

            [JsonProperty("saveInterval")]
            public int? SaveInterval { get; set; }

            [JsonProperty("backupCount")]
            public int? BackupCount { get; set; }

            [JsonProperty("backupIntervalShort")]
            public int? BackupIntervalShort { get; set; }

            [JsonProperty("backupIntervalLong")]
            public int? BackupIntervalLong { get; set; }

            [JsonProperty("logDirectory")]
            public string LogDirectory { get; set; }
        }
    }
}
