using System;

namespace ValheimServerGUI.Game
{
    public class PlayerInfo
    {
        // I'm assuming you can't put a tab in your character's name...
        private const char Separator = '\t';

        public string PlayerName { get; set; }

        public string SteamId { get; set; }

        public DateTimeOffset LastOnline { get; set; }

        public PlayerStatus PlayerStatus { get; set; }

        public static PlayerInfo Parse(string info)
        {
            var parts = info.Split(Separator);

            return new PlayerInfo
            {
                PlayerName = parts[1],
                SteamId = parts[2],
                LastOnline = DateTimeOffset.Parse(parts[3]),
            };
        }

        public override string ToString()
        {
            // todo: Sanitize PlayerName input
            return string.Join(Separator, PlayerName, SteamId, LastOnline);
        }
    }
}
