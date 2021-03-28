﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using ValheimServerGUI.Tools.Data;

namespace ValheimServerGUI.Game
{
    public class PlayerInfo : IPrimaryKeyEntity
    {
        [JsonIgnore]
        public string Key => this.SteamId;

        /// <summary>
        /// The character's name as it appears within Valheim
        /// </summary>
        [JsonProperty("name")]
        public string PlayerName { get; set; }

        /// <summary>
        /// The ID used to identify the player on Steam.
        /// The same Steam player may log in with multiple characters.
        /// </summary>
        [JsonProperty("steamId")]
        public string SteamId { get; set; }

        /// <summary>
        /// Could we confidently match up the player's name and steamId?
        /// </summary>
        [JsonProperty("steamIdConfident")]
        public bool SteamIdConfident { get; set; }

        /// <summary>
        /// If we could not confidently match up the connecting player name with a steamId,
        /// then keep track of any possible ids. Will try to resolve the correct one over time.
        /// This list will also contain the value chosen for SteamId.
        /// </summary>
        [JsonProperty("steamIdAlternates", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> SteamIdAlternates { get; set; }

        /// <summary>
        /// The last time the player logged on to the server.
        /// </summary>
        [JsonProperty("lastStatusChange")]
        public DateTimeOffset LastStatusChange { get; set; }

        /// <summary>
        /// The player's current status on the server.
        /// </summary>
        [JsonIgnore]
        public PlayerStatus PlayerStatus { get; set; }

        /// <summary>
        /// The player's current object id in-game. Changes with each session.
        /// </summary>
        [JsonIgnore]
        public string ZdoId { get; set; }
    }
}
