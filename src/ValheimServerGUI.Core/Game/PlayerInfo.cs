using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using ValheimServerGUI.Tools.Data;

namespace ValheimServerGUI.Game
{
    public class PlayerInfo : IPrimaryKeyEntity
    {
        [JsonIgnore]
        public string Key => $"{Platform}:{PlayerId}";

        /// <summary>
        /// The platform that this player is playing Valheim on.
        /// </summary>
        [JsonProperty("platform")]
        public string? Platform { get; set; }

        /// <summary>
        /// The exact platform token as the game printed it in its crossplay log line (e.g. "Xbox",
        /// "PlayStation"/"Playstation", "Switch"), preserved verbatim so admin/ban/permit list-file entries
        /// are written with the binary's own casing (<c>ZNet.ListContainsId</c> matches non-Steam IDs
        /// case-sensitively). <see cref="Platform"/> stays the normalized value for UI/icons/name-lookup.
        /// Null for records created before this field existed or added by ID without a captured token; the
        /// access-list service then falls back to <see cref="Platform"/>.
        /// </summary>
        [JsonProperty("platformRaw", NullValueHandling = NullValueHandling.Ignore)]
        public string? PlatformRaw { get; set; }

        /// <summary>
        /// The ID used to identify the player on this platform.
        /// The same player may log in with multiple characters.
        /// </summary>
        [JsonProperty("playerId")]
        public string? PlayerId { get; set; }

        /// <summary>
        /// The player's username on the specified platform.
        /// </summary>
        [JsonProperty("playerName")]
        public string? PlayerName { get; set; }

        /// <summary>
        /// The last time the player logged on to the server.
        /// </summary>
        [JsonProperty("lastStatusChange")]
        public DateTimeOffset LastStatusChange { get; set; }

        [JsonProperty("lastStatusCharacter")]
        public string? LastStatusCharacter { get; set; }

        /// <summary>
        /// A list of characters that this player has played in Valheim.
        /// </summary>
        [JsonProperty("characters")]
        public List<CharacterInfo>? Characters { get; set; }

        /// <summary>
        /// The player's current status on the server.
        /// </summary>
        [JsonIgnore]
        public PlayerStatus PlayerStatus { get; set; }

        /// <summary>
        /// The player's current object id in-game. Changes with each session.
        /// </summary>
        [JsonIgnore]
        public string? ZdoId { get; set; }

        public CharacterInfo AddCharacter(string characterName, bool matchConfident = true)
        {
            Characters ??= new();

            var character = Characters.FirstOrDefault(c => c.CharacterName == characterName);
            if (character == null)
            {
                character = new CharacterInfo
                {
                    CharacterName = characterName,
                };
                Characters.Add(character);
            }

            character.MatchConfident = matchConfident;
            return character;
        }

        public bool TryGetCharacter(string characterName, out CharacterInfo? character)
        {
            character = Characters?.FirstOrDefault(c => c.CharacterName == characterName);
            return character != null;
        }

        /// <summary>
        /// Records the time a specific character was last active (login or session end). No-op if the
        /// character isn't in the list. Backs the per-character "Since" column in Player Details.
        /// </summary>
        public void TouchCharacterLastSeen(string? characterName, DateTimeOffset timestamp)
        {
            if (!string.IsNullOrWhiteSpace(characterName)
                && TryGetCharacter(characterName!, out var character) && character != null)
            {
                character.LastSeen = timestamp;
            }
        }

        public class CharacterInfo
        {
            /// <summary>
            /// The character's name as it appears within Valheim.
            /// </summary>
            [JsonProperty("characterName")]
            public string? CharacterName { get; set; }

            /// <summary>
            /// Could we confidently match up the player's name and platformId?
            /// </summary>
            [JsonProperty("matchConfident")]
            public bool MatchConfident { get; set; }

            /// <summary>
            /// The last time this character was active on the server (login or session end). Added in v3.0
            /// and optional: characters recorded by earlier versions leave this null (a blank "Since").
            /// </summary>
            [JsonProperty("lastSeen", NullValueHandling = NullValueHandling.Ignore)]
            public DateTimeOffset? LastSeen { get; set; }
        }
    }
}
