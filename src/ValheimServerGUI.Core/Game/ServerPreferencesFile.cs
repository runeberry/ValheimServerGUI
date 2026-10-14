using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ValheimServerGUI.Game
{
    public class ServerPreferencesFile
    {
        [JsonProperty("profileName")]
        public string?ProfileName { get; set; }

        [JsonProperty("lastSaved")]
        public DateTime? LastSaved { get; set; }

        [JsonProperty("name")]
        public string?Name { get; set; }

        [JsonProperty("password")]
        public string?Password { get; set; }

        [JsonProperty("world")]
        public string?WorldName { get; set; }

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

        [JsonProperty("autoStart")]
        public bool? AutoStart { get; set; }

        [JsonProperty("additionalArgs")]
        public string?AdditionalArgs { get; set; }

        [JsonProperty("valheimServerPath")]
        public string?ServerExePath { get; set; }

        [JsonProperty("valheimSaveDataFolder")]
        public string?SaveDataFolderPath { get; set; }

        [JsonProperty("writeServerLogsToFile")]
        public bool? WriteServerLogsToFile { get; set; }

        [JsonProperty("usePermittedList")]
        public bool? UsePermittedList { get; set; }

        /// <summary>Per-player roles keyed by <c>"{Platform}:{PlayerId}"</c>. Omitted when the profile has none.</summary>
        [JsonProperty("playerRoles", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, PlayerRoleFileEntry>? PlayerRoles { get; set; }
    }

    /// <summary>The persisted form of one role assignment: a lowercase role token + optional raw platform.</summary>
    public class PlayerRoleFileEntry
    {
        [JsonProperty("role")]
        public string? Role { get; set; }

        [JsonProperty("platformRaw", NullValueHandling = NullValueHandling.Ignore)]
        public string? PlatformRaw { get; set; }
    }
}
