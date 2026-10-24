using System;
using System.Collections.Generic;
using System.Linq;

namespace ValheimServerGUI.Game
{
    /// <summary>A difficulty preset: a named set of World Preferences values.</summary>
    public sealed record WorldGenPreset(string Token, WorldGenSettings Settings);

    /// <summary>
    /// How the game would name a set of world settings: the preset they contain, and whether they add anything
    /// beyond it (shown as "Hard+"). <see cref="Preset"/> is null when no preset fits (Custom).
    /// </summary>
    public sealed record WorldGenPresetLabel(string? Preset, bool HasExtras);

    public static class WorldGenPresets
    {
        public const string Normal = "normal";
        public const string Casual = "casual";
        public const string Easy = "easy";
        public const string Hard = "hard";
        public const string Hardcore = "hardcore";
        public const string Immersive = "immersive";
        public const string Hammer = "hammer";

        /// <summary>
        /// The game's presets as World Preferences values, in the game's button order (which breaks ties when naming
        /// settings). Each one stores exactly the keys of the game's preset button in the start scene. Normal is the
        /// game's "Default" button: everything at Normal, no keys.
        /// </summary>
        public static readonly IReadOnlyList<WorldGenPreset> All = new List<WorldGenPreset>
        {
            Preset(Easy, new()
            {
                [WorldGenModifiers.Combat] = WorldGenModifiers.Values.CombatEasy,
                [WorldGenModifiers.Raids] = WorldGenModifiers.Values.RaidsLess,
            }),
            Preset(Hard, new()
            {
                [WorldGenModifiers.Combat] = WorldGenModifiers.Values.CombatHard,
                [WorldGenModifiers.Raids] = WorldGenModifiers.Values.RaidsMore,
            }),
            Preset(Hardcore, new()
            {
                [WorldGenModifiers.Combat] = WorldGenModifiers.Values.CombatVeryHard,
                [WorldGenModifiers.DeathPenalty] = WorldGenModifiers.Values.DeathPenaltyHardcore,
                [WorldGenModifiers.Raids] = WorldGenModifiers.Values.RaidsMore,
                [WorldGenModifiers.Portals] = WorldGenModifiers.Values.PortalsHard,
            }, WorldGenKeys.NoMap),
            Preset(Casual, new()
            {
                [WorldGenModifiers.Combat] = WorldGenModifiers.Values.CombatVeryEasy,
                [WorldGenModifiers.DeathPenalty] = WorldGenModifiers.Values.DeathPenaltyCasual,
                [WorldGenModifiers.Resources] = WorldGenModifiers.Values.ResourcesMore,
                [WorldGenModifiers.Raids] = WorldGenModifiers.Values.RaidsNone,
                [WorldGenModifiers.Portals] = WorldGenModifiers.Values.PortalsCasual,
            }, WorldGenKeys.PassiveMobs, WorldGenKeys.PlayerEvents),
            Preset(Hammer, new()
            {
                [WorldGenModifiers.Raids] = WorldGenModifiers.Values.RaidsNone,
            }, WorldGenKeys.NoBuildCost, WorldGenKeys.PassiveMobs),
            Preset(Immersive, new()
            {
                [WorldGenModifiers.Portals] = WorldGenModifiers.Values.PortalsVeryHard,
            }, WorldGenKeys.NoMap, WorldGenKeys.Fire),
            Preset(Normal, new()),
        };

        public static WorldGenPreset? Find(string? token) => All.FirstOrDefault(p => p.Token == token);

        /// <summary>
        /// Names settings the way the game's world list does (<c>ServerOptionsGUI.GetWorldModifierSummary</c>): of the
        /// presets whose keys are all stored, the one with the most keys (ties go to the earlier button); "extras" when
        /// the keys left over still make up a modifier value or a world key. Settings at all defaults are Normal.
        /// </summary>
        public static WorldGenPresetLabel Label(WorldGenSettings settings)
        {
            if (settings.IsDefault) return new WorldGenPresetLabel(Normal, HasExtras: false);

            var gameKeys = settings.ToGameKeys();
            WorldGenPreset? best = null;
            var bestCount = 0;
            foreach (var preset in All.Where(p => p.Token != Normal))
            {
                var presetKeys = preset.Settings.ToGameKeys();
                if (presetKeys.Count > bestCount && presetKeys.All(gameKeys.Contains))
                {
                    best = preset;
                    bestCount = presetKeys.Count;
                }
            }

            if (best is null) return new WorldGenPresetLabel(null, HasExtras: true);

            var leftover = gameKeys.Except(best.Settings.ToGameKeys(), StringComparer.OrdinalIgnoreCase);
            var hasExtras = !WorldGenSettings.FromGameKeys(leftover, out _).IsDefault;
            return new WorldGenPresetLabel(best.Token, hasExtras);
        }

        private static WorldGenPreset Preset(string token, Dictionary<string, string> modifiers, params string[] keys)
            => new(token, new WorldGenSettings(modifiers, keys));
    }
}
