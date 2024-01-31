using System.Collections.Generic;

namespace ValheimServerGUI.Game
{
    public static class WorldGenPresets
    {
        public const string Normal = "normal";
        public const string Casual = "casual";
        public const string Easy = "easy";
        public const string Hard = "hard";
        public const string Hardcore = "hardcore";
        public const string Immersive = "immersive";
        public const string Hammer = "hammer";

        public static readonly IReadOnlyList<string> All = new List<string>()
        {
            Normal,
            Casual,
            Easy,
            Hard,
            Hardcore,
            Immersive,
            Hammer,
        };
    }
}
