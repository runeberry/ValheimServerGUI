using Newtonsoft.Json;
using System;

namespace ValheimServerGUI.Game
{
    public class ServerPreferencesFile
    {
        [JsonProperty("profileName")]
        public string ProfileName { get; set; }

        [JsonProperty("lastSaved")]
        public DateTime? LastSaved { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("world")]
        public string WorldName { get; set; }

        [JsonProperty("community")]
        public bool? Community { get; set; }

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

        [JsonProperty("startWithWindows")]
        public bool? StartWithWindows { get; set; }
    }
}
