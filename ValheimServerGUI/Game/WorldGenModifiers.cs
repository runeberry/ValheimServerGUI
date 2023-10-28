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

        public static class Values
        {
            public const string CombatVeryEasy = "veryeasy";
            public const string CombatEasy = "easy";
            public const string CombatHard = "hard";
            public const string CombatVeryHard = "veryhard";

            public const string DeathPenaltyCasual = "casual";
            public const string DeathPenaltyVeryEasy = "veryeasy";
            public const string DeathPenaltyEasy = "easy";
            public const string DeathPenaltyHard = "hard";
            public const string DeathPenaltyHardcore = "hardcore";

            public const string ResourcesMuchLess = "muchless";
            public const string ResourcesLess = "less";
            public const string ResourcesMore = "more";
            public const string ResourcesMuchMore = "muchmore";
            public const string ResourcesMost = "most";

            public const string RaidsNone = "none";
            public const string RaidsMuchLess = "muchless";
            public const string RaidsLess = "less";
            public const string RaidsMore = "more";
            public const string RaidsMuchMore = "muchmore";

            public const string PortalsCasual = "casual";
            public const string PortalsHard = "hard";
            public const string PortalsVeryHard = "veryhard";
        }

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
                    Values.CombatVeryEasy,
                    Values.CombatEasy,
                    Values.CombatHard,
                    Values.CombatVeryHard,
                }
            },
            {
                DeathPenalty,
                new[]
                {
                    Values.DeathPenaltyCasual,
                    Values.DeathPenaltyVeryEasy,
                    Values.DeathPenaltyEasy,
                    Values.DeathPenaltyHard,
                    Values.DeathPenaltyHardcore,
                }
            },
            {
                Resources,
                new[]
                {
                    Values.ResourcesMuchLess,
                    Values.ResourcesLess,
                    Values.ResourcesMore,
                    Values.ResourcesMuchMore,
                    Values.ResourcesMost,
                }
            },
            {
                Raids,
                new[]
                {
                    Values.RaidsNone,
                    Values.RaidsMuchLess,
                    Values.RaidsLess,
                    Values.RaidsMore,
                    Values.RaidsMuchMore,
                }
            },
            {
                Portals,
                new[]
                {
                    Values.PortalsCasual,
                    Values.PortalsHard,
                    Values.PortalsVeryHard,
                }
            },
        };
    }
}
