using System.Collections.Generic;
using System.Linq;
using ValheimServerGUI.Game;

namespace ValheimServerGUI.App.ViewModels.Dialogs;

/// <summary>
/// UI display names + help text for the raw world-gen CLI tokens (presets, modifiers, modifier values, and
/// keys). The Core model stores only the tokens Valheim's command line expects (e.g. <c>deathpenalty</c> =
/// <c>veryhard</c>); this maps them to the friendly names and descriptions the WinForms app showed, and back,
/// so the World Preferences dialog never surfaces raw tokens. Sentinels: a modifier set to "Normal" persists
/// as no value (omitted); the preset set to <see cref="WorldPreferencesViewModel.CustomPreset"/> persists as
/// no preset.
/// </summary>
internal static class WorldGenDisplay
{
    /// <summary>The "unset" display shown for a modifier with no override (persists as null).</summary>
    public const string NormalModifier = "Normal";

    // --- Presets (ordered as shown; display <-> token) ---
    private static readonly (string Display, string Token)[] PresetPairs =
    {
        ("Custom (No Preset)", WorldPreferencesViewModel.CustomPreset),
        ("Easy", WorldGenPresets.Easy),
        ("Normal", WorldGenPresets.Normal),
        ("Hard", WorldGenPresets.Hard),
        ("Hardcore", WorldGenPresets.Hardcore),
        ("Casual", WorldGenPresets.Casual),
        ("Hammer Mode (Creative)", WorldGenPresets.Hammer),
        ("Immersive", WorldGenPresets.Immersive),
    };

    public static IReadOnlyList<string> PresetDisplays { get; } = PresetPairs.Select(p => p.Display).ToList();

    public static string PresetDisplay(string token) =>
        PresetPairs.FirstOrDefault(p => p.Token == token).Display ?? PresetPairs[0].Display;

    public static string PresetToken(string display) =>
        PresetPairs.FirstOrDefault(p => p.Display == display).Token ?? WorldPreferencesViewModel.CustomPreset;

    // --- Modifier names + help ---
    private static readonly Dictionary<string, string> ModifierNames = new()
    {
        [WorldGenModifiers.Combat] = "Combat",
        [WorldGenModifiers.DeathPenalty] = "Death Penalty",
        [WorldGenModifiers.Resources] = "Resource Rate",
        [WorldGenModifiers.Raids] = "Raids",
        [WorldGenModifiers.Portals] = "Portals",
    };

    private static readonly Dictionary<string, string> ModifierHelps = new()
    {
        [WorldGenModifiers.Combat] = "Governs how much damage you give and take. Also governs how likely you are to encounter higher leveled enemies, and how dangerous they are.",
        [WorldGenModifiers.DeathPenalty] = "Governs what happens when you die. See the wiki for an explanation of the different options.",
        [WorldGenModifiers.Resources] = "Governs the amount of resources you gain from the world and from enemies.",
        [WorldGenModifiers.Raids] = "Governs how often enemies may raid your base.",
        [WorldGenModifiers.Portals] = "Changes how portals work in the game.",
    };

    public static string ModifierName(string key) => ModifierNames.TryGetValue(key, out var v) ? v : key;
    public static string ModifierHelp(string key) => ModifierHelps.TryGetValue(key, out var v) ? v : string.Empty;

    // --- Modifier values (ordered per modifier, with the "Normal" sentinel positioned as in WinForms).
    //     A null token is the "Normal" (omitted) value. ---
    private static readonly Dictionary<string, (string Display, string? Token)[]> ModifierValues = new()
    {
        [WorldGenModifiers.Combat] = new (string, string?)[]
        {
            ("Very Easy", WorldGenModifiers.Values.CombatVeryEasy),
            ("Easy", WorldGenModifiers.Values.CombatEasy),
            (NormalModifier, null),
            ("Hard", WorldGenModifiers.Values.CombatHard),
            ("Very Hard", WorldGenModifiers.Values.CombatVeryHard),
        },
        [WorldGenModifiers.DeathPenalty] = new (string, string?)[]
        {
            ("Casual", WorldGenModifiers.Values.DeathPenaltyCasual),
            ("Very Easy", WorldGenModifiers.Values.DeathPenaltyVeryEasy),
            ("Easy", WorldGenModifiers.Values.DeathPenaltyEasy),
            (NormalModifier, null),
            ("Hard", WorldGenModifiers.Values.DeathPenaltyHard),
            ("Hardcore", WorldGenModifiers.Values.DeathPenaltyHardcore),
        },
        [WorldGenModifiers.Resources] = new (string, string?)[]
        {
            ("Much Less (0.5x)", WorldGenModifiers.Values.ResourcesMuchLess),
            ("Less (0.75x)", WorldGenModifiers.Values.ResourcesLess),
            (NormalModifier, null),
            ("More (1.5x)", WorldGenModifiers.Values.ResourcesMore),
            ("Much More (2x)", WorldGenModifiers.Values.ResourcesMuchMore),
            ("Most (3x)", WorldGenModifiers.Values.ResourcesMost),
        },
        [WorldGenModifiers.Raids] = new (string, string?)[]
        {
            ("None", WorldGenModifiers.Values.RaidsNone),
            ("Much Less", WorldGenModifiers.Values.RaidsMuchLess),
            ("Less", WorldGenModifiers.Values.RaidsLess),
            (NormalModifier, null),
            ("More", WorldGenModifiers.Values.RaidsMore),
            ("Much More", WorldGenModifiers.Values.RaidsMuchMore),
        },
        [WorldGenModifiers.Portals] = new (string, string?)[]
        {
            ("Casual (Portal items)", WorldGenModifiers.Values.PortalsCasual),
            (NormalModifier, null),
            ("Hard (No boss portals)", WorldGenModifiers.Values.PortalsHard),
            ("Very Hard (No portals)", WorldGenModifiers.Values.PortalsVeryHard),
        },
    };

    public static IReadOnlyList<string> ModifierValueDisplays(string key) =>
        ModifierValues[key].Select(v => v.Display).ToList();

    /// <summary>Maps a stored token (or null for "Normal") to its display name for the given modifier.</summary>
    public static string ModifierValueDisplay(string key, string? token)
    {
        foreach (var (display, t) in ModifierValues[key])
            if (t == token) return display;
        return NormalModifier;
    }

    /// <summary>Maps a display name back to its stored token (null for "Normal") for the given modifier.</summary>
    public static string? ModifierValueToken(string key, string display)
    {
        foreach (var (d, token) in ModifierValues[key])
            if (d == display) return token;
        return null;
    }

    // --- Keys: display names + help ---
    private static readonly Dictionary<string, string> KeyNames = new()
    {
        [WorldGenKeys.NoBuildCost] = "No build cost",
        [WorldGenKeys.PlayerEvents] = "Player based raids",
        [WorldGenKeys.PassiveMobs] = "Passive enemies",
        [WorldGenKeys.NoMap] = "No map",
        [WorldGenKeys.Fire] = "Fire hazards",
    };

    private static readonly Dictionary<string, string> KeyHelps = new()
    {
        [WorldGenKeys.NoBuildCost] = "Build pieces require no materials to build. You still need to discover recipes as per usual.",
        [WorldGenKeys.PlayerEvents] = "Raids are based on the progress of each individual player, rather than on which bosses have been killed on the server. This setting is recommended if you want the game to be slightly friendlier to players with different progress.",
        [WorldGenKeys.PassiveMobs] = "Enemies won't attack until you provoke them.",
        [WorldGenKeys.NoMap] = "You will not have access to the map or the minimap. This makes the game harder than intended.",
        [WorldGenKeys.Fire] = "Wood can catch fire and spread throughout the whole world, not just in the Ashlands.",
    };

    public static string KeyName(string key) => KeyNames.TryGetValue(key, out var v) ? v : key;
    public static string KeyHelp(string key) => KeyHelps.TryGetValue(key, out var v) ? v : string.Empty;
}
