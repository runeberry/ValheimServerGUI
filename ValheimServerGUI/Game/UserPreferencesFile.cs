﻿using Newtonsoft.Json;
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
        // (jb, 2/19/23) This field was previously recorded, but never used.
        //[JsonProperty("valheimGamePath")]
        //public string ValheimGamePath { get; set; }

        [JsonProperty("valheimServerPath")]
        public string ServerExePath { get; set; }

        [JsonProperty("valheimSaveDataFolder")]
        public string SaveDataFolderPath { get; set; }

        [JsonProperty("startWithWindows")]
        public bool? StartWithWindows { get; set; }

        [JsonProperty("startServerAutomatically"), Obsolete("Moved to server preferences", true)]
        public bool? StartServerAutomatically { get; set; }

        [JsonProperty("startMinimized")]
        public bool? StartMinimized { get; set; }

        [JsonProperty("checkServerRunning"), Obsolete("Removed with multi-server support", true)]
        public bool? CheckServerRunning { get; set; }

        [JsonProperty("checkForUpdates")]
        public bool? CheckForUpdates { get; set; }

        [JsonProperty("saveProfileOnStart")]
        public bool? SaveProfileOnStart { get; set; }

        [JsonProperty("writeApplicationLogsToFile")]
        public bool? WriteApplicationLogsToFile { get; set; }

        [JsonProperty("enablePasswordValidation")]
        public bool? EnablePasswordValidation { get; set; }

        [JsonProperty("servers")]
        public List<ServerPreferencesFile> Servers { get; set; }

        [JsonProperty("worlds")]
        public List<WorldPreferencesFile> Worlds { get; set; }
    }
}
