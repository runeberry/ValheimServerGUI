using System.Collections.Generic;

namespace ValheimServerGUI.Tools.Models
{
    public static class PlayerPlatforms
    {
        public const string Steam = "Steam";
        public const string Xbox = "Xbox";

        public static readonly HashSet<string> All = new()
        {
            Steam,
            Xbox,
        };
    }
}
