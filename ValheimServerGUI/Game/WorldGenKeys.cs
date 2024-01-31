using System.Collections.Generic;

namespace ValheimServerGUI.Game
{
    public static class WorldGenKeys
    {
        public const string NoBuildCost = "nobuildcost";
        public const string PlayerEvents = "playerevents";
        public const string PassiveMobs = "passivemobs";
        public const string NoMap = "nomap";

        public static readonly IReadOnlyList<string> All = new List<string>
        {
            NoBuildCost,
            PlayerEvents,
            PassiveMobs,
            NoMap,
        };
    }
}
