using System.Collections.Generic;

namespace ValheimServerGUI.Game
{
    public static class WorldGenModifiers
    {
        public const string Combat = "combat";
        public const string DeathPenalty = "deathpenalty";
        public const string Resources = "resources";
        public const string Raids = "raids";
        public const string Portals = "portals";

        public static readonly IReadOnlyList<string> All = new List<string>()
        {
            Combat,
            DeathPenalty,
            Resources,
            Raids,
            Portals,
        };

        public static readonly IReadOnlyDictionary<string, string[]> AllowedValues = new Dictionary<string, string[]>()
        {
            {
                Combat,
                new[]
                {
                    "veryeasy",
                    "easy",
                    "hard",
                    "veryhard",
                }
            },
            {
                DeathPenalty,
                new[]
                {
                    "casual",
                    "veryeasy",
                    "easy",
                    "hard",
                    "hardcore",
                }
            },
            {
                Resources,
                new[]
                {
                    "muchless",
                    "less",
                    "more",
                    "muchmore",
                    "most",
                }
            },
            {
                Raids,
                new[]
                {
                    "none",
                    "muchless",
                    "less",
                    "more",
                    "muchmore",
                }
            },
            {
                Portals,
                new[]
                {
                    "casual",
                    "hard",
                    "veryhard",
                }
            },
        };
    }
}
