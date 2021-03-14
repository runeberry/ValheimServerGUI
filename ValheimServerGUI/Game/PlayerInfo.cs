using System;

namespace ValheimServerGUI.Game
{
    public class PlayerInfo
    {
        // I'm assuming you can't put a tab in your character's name...
        private const char Separator = '\t';

        /// <summary>
        /// The character's name as it appears within Valheim
        /// </summary>
        public string PlayerName { get; set; }

        /// <summary>
        /// The ID used to identify the player on Steam.
        /// The same Steam player may log in with multiple characters.
        /// </summary>
        public string SteamId { get; set; }

        /// <summary>
        /// The last time the player logged on to the server.
        /// </summary>
        public DateTimeOffset LastStatusChange { get; set; }

        /// <summary>
        /// The player's current status on the server.
        /// </summary>
        public PlayerStatus PlayerStatus { get; set; }

        public static PlayerInfo Parse(string info)
        {
            var parts = info.Split(Separator);

            return new PlayerInfo
            {
                PlayerName = parts[1],
                SteamId = parts[2],
                LastStatusChange = DateTimeOffset.Parse(parts[3]),
            };
        }

        public override string ToString()
        {
            // todo: Sanitize PlayerName input
            return string.Join(Separator,
                PlayerName,
                SteamId,
                LastStatusChange);
        }
    }
}
