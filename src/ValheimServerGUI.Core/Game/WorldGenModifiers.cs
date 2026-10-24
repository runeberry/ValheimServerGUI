using System;
using System.Collections.Generic;
using System.Linq;

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

        /// <summary>
        /// The world keys each modifier value stores, in the game's slider order, copied from the game's start scene
        /// (the <c>KeySlider</c> settings under <c>StartGui_ServerOptions</c>). "Normal" stores no keys, so it is not
        /// listed. Keys are lowercase, as the game writes them.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, IReadOnlyList<(string Value, string[] GameKeys)>> Settings =
            new Dictionary<string, IReadOnlyList<(string, string[])>>
            {
                [Combat] = new (string, string[])[]
                {
                    (Values.CombatVeryEasy, new[] { "playerdamage 125", "enemydamage 50", "enemyspeedsize 90" }),
                    (Values.CombatEasy, new[] { "playerdamage 110", "enemydamage 75", "enemyspeedsize 95" }),
                    (Values.CombatHard, new[] { "playerdamage 85", "enemydamage 150", "enemyspeedsize 110", "enemyleveluprate 120" }),
                    (Values.CombatVeryHard, new[] { "playerdamage 70", "enemydamage 200", "enemyspeedsize 120", "enemyleveluprate 140" }),
                },
                [DeathPenalty] = new (string, string[])[]
                {
                    (Values.DeathPenaltyCasual, new[] { "deathkeepequip", "skillreductionrate 15" }),
                    (Values.DeathPenaltyVeryEasy, new[] { "skillreductionrate 15" }),
                    (Values.DeathPenaltyEasy, new[] { "skillreductionrate 50" }),
                    (Values.DeathPenaltyHard, new[] { "deathdeleteunequipped", "skillreductionrate 150" }),
                    (Values.DeathPenaltyHardcore, new[] { "deathdeleteitems", "deathskillsreset" }),
                },
                [Resources] = new (string, string[])[]
                {
                    (Values.ResourcesMuchLess, new[] { "resourcerate 50" }),
                    (Values.ResourcesLess, new[] { "resourcerate 75" }),
                    (Values.ResourcesMore, new[] { "resourcerate 150" }),
                    (Values.ResourcesMuchMore, new[] { "resourcerate 200" }),
                    (Values.ResourcesMost, new[] { "resourcerate 300" }),
                },
                [Raids] = new (string, string[])[]
                {
                    (Values.RaidsNone, new[] { "eventrate 0" }),
                    (Values.RaidsMuchLess, new[] { "eventrate 200" }),
                    (Values.RaidsLess, new[] { "eventrate 150" }),
                    (Values.RaidsMore, new[] { "eventrate 60" }),
                    (Values.RaidsMuchMore, new[] { "eventrate 30" }),
                },
                [Portals] = new (string, string[])[]
                {
                    (Values.PortalsCasual, new[] { "teleportall" }),
                    (Values.PortalsHard, new[] { "nobossportals" }),
                    (Values.PortalsVeryHard, new[] { "noportals" }),
                },
            };

        public static readonly IReadOnlyDictionary<string, string[]> AllowedValues =
            Settings.ToDictionary(s => s.Key, s => s.Value.Select(v => v.Value).ToArray());

        /// <summary>The world keys one modifier value stores (empty for "Normal" or an unknown value).</summary>
        public static IReadOnlyList<string> GameKeys(string modifier, string? value)
            => Settings.TryGetValue(modifier, out var settings)
                ? settings.FirstOrDefault(s => s.Value == value).GameKeys ?? Array.Empty<string>()
                : Array.Empty<string>();
    }
}
