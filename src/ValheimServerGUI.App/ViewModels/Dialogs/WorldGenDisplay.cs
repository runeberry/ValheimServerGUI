using System.Collections.Generic;
using System.Linq;
using ValheimServerGUI.Game;
using ValheimServerGUI.Localization;

namespace ValheimServerGUI.App.ViewModels.Dialogs;

/// <summary>
/// UI display names + help text for the raw world-gen CLI tokens (presets, modifiers, modifier values, and
/// keys). The Core model stores only the tokens Valheim's command line expects (e.g. <c>deathpenalty</c> =
/// <c>veryhard</c>); this maps them to the friendly names and descriptions the WinForms app showed, and back,
/// so the World Preferences dialog never surfaces raw tokens. Each mapping is one token↔display table used in
/// both directions; the tables are built on each call so they always read the current UI culture's text.
/// Sentinels: a modifier set to "Normal" is the null token and persists as no value (omitted); the preset set
/// to <see cref="WorldPreferencesViewModel.CustomPreset"/> persists as no preset.
/// </summary>
internal static class WorldGenDisplay
{
    /// <summary>The "unset" display shown for a modifier with no override (the null token).</summary>
    public static string NormalModifier => Strings.WorldGen_Value_Normal;

    // --- Presets (ordered as shown; display <-> token) ---
    private static (string Display, string Token)[] PresetPairs() => new[]
    {
        (Strings.WorldGen_Preset_Custom, WorldPreferencesViewModel.CustomPreset),
        (Strings.WorldGen_Preset_Easy, WorldGenPresets.Easy),
        (Strings.WorldGen_Preset_Normal, WorldGenPresets.Normal),
        (Strings.WorldGen_Preset_Hard, WorldGenPresets.Hard),
        (Strings.WorldGen_Preset_Hardcore, WorldGenPresets.Hardcore),
        (Strings.WorldGen_Preset_Casual, WorldGenPresets.Casual),
        (Strings.WorldGen_Preset_Hammer, WorldGenPresets.Hammer),
        (Strings.WorldGen_Preset_Immersive, WorldGenPresets.Immersive),
    };

    public static IReadOnlyList<string> PresetDisplays => PresetPairs().Select(p => p.Display).ToList();

    public static string PresetDisplay(string token)
    {
        var pairs = PresetPairs();
        return pairs.FirstOrDefault(p => p.Token == token).Display ?? pairs[0].Display;
    }

    public static string PresetToken(string display) =>
        PresetPairs().FirstOrDefault(p => p.Display == display).Token ?? WorldPreferencesViewModel.CustomPreset;

    // --- Modifier names + help ---
    public static string ModifierName(string key) => key switch
    {
        WorldGenModifiers.Combat => Strings.WorldGen_Combat_Name,
        WorldGenModifiers.DeathPenalty => Strings.WorldGen_DeathPenalty_Name,
        WorldGenModifiers.Resources => Strings.WorldGen_Resources_Name,
        WorldGenModifiers.Raids => Strings.WorldGen_Raids_Name,
        WorldGenModifiers.Portals => Strings.WorldGen_Portals_Name,
        _ => key,
    };

    public static string ModifierHelp(string key) => key switch
    {
        WorldGenModifiers.Combat => Strings.WorldGen_Combat_Help,
        WorldGenModifiers.DeathPenalty => Strings.WorldGen_DeathPenalty_Help,
        WorldGenModifiers.Resources => Strings.WorldGen_Resources_Help,
        WorldGenModifiers.Raids => Strings.WorldGen_Raids_Help,
        WorldGenModifiers.Portals => Strings.WorldGen_Portals_Help,
        _ => string.Empty,
    };

    // --- Modifier values (ordered per modifier, with the "Normal" sentinel positioned as in WinForms).
    //     A null token is the "Normal" (omitted) value. ---
    private static (string Display, string? Token)[] ModifierValues(string key) => key switch
    {
        WorldGenModifiers.Combat => new (string, string?)[]
        {
            (Strings.WorldGen_Combat_VeryEasy, WorldGenModifiers.Values.CombatVeryEasy),
            (Strings.WorldGen_Combat_Easy, WorldGenModifiers.Values.CombatEasy),
            (NormalModifier, null),
            (Strings.WorldGen_Combat_Hard, WorldGenModifiers.Values.CombatHard),
            (Strings.WorldGen_Combat_VeryHard, WorldGenModifiers.Values.CombatVeryHard),
        },
        WorldGenModifiers.DeathPenalty => new (string, string?)[]
        {
            (Strings.WorldGen_DeathPenalty_Casual, WorldGenModifiers.Values.DeathPenaltyCasual),
            (Strings.WorldGen_DeathPenalty_VeryEasy, WorldGenModifiers.Values.DeathPenaltyVeryEasy),
            (Strings.WorldGen_DeathPenalty_Easy, WorldGenModifiers.Values.DeathPenaltyEasy),
            (NormalModifier, null),
            (Strings.WorldGen_DeathPenalty_Hard, WorldGenModifiers.Values.DeathPenaltyHard),
            (Strings.WorldGen_DeathPenalty_Hardcore, WorldGenModifiers.Values.DeathPenaltyHardcore),
        },
        WorldGenModifiers.Resources => new (string, string?)[]
        {
            (Strings.WorldGen_Resources_MuchLess, WorldGenModifiers.Values.ResourcesMuchLess),
            (Strings.WorldGen_Resources_Less, WorldGenModifiers.Values.ResourcesLess),
            (NormalModifier, null),
            (Strings.WorldGen_Resources_More, WorldGenModifiers.Values.ResourcesMore),
            (Strings.WorldGen_Resources_MuchMore, WorldGenModifiers.Values.ResourcesMuchMore),
            (Strings.WorldGen_Resources_Most, WorldGenModifiers.Values.ResourcesMost),
        },
        WorldGenModifiers.Raids => new (string, string?)[]
        {
            (Strings.WorldGen_Raids_None, WorldGenModifiers.Values.RaidsNone),
            (Strings.WorldGen_Raids_MuchLess, WorldGenModifiers.Values.RaidsMuchLess),
            (Strings.WorldGen_Raids_Less, WorldGenModifiers.Values.RaidsLess),
            (NormalModifier, null),
            (Strings.WorldGen_Raids_More, WorldGenModifiers.Values.RaidsMore),
            (Strings.WorldGen_Raids_MuchMore, WorldGenModifiers.Values.RaidsMuchMore),
        },
        WorldGenModifiers.Portals => new (string, string?)[]
        {
            (Strings.WorldGen_Portals_Casual, WorldGenModifiers.Values.PortalsCasual),
            (NormalModifier, null),
            (Strings.WorldGen_Portals_Hard, WorldGenModifiers.Values.PortalsHard),
            (Strings.WorldGen_Portals_VeryHard, WorldGenModifiers.Values.PortalsVeryHard),
        },
        _ => throw new KeyNotFoundException(key),
    };

    public static IReadOnlyList<string> ModifierValueDisplays(string key) =>
        ModifierValues(key).Select(v => v.Display).ToList();

    /// <summary>Maps a stored token (or null for "Normal") to its display name for the given modifier.</summary>
    public static string ModifierValueDisplay(string key, string? token)
    {
        foreach (var (display, t) in ModifierValues(key))
            if (t == token) return display;
        return NormalModifier;
    }

    /// <summary>Maps a display name back to its stored token (null for "Normal") for the given modifier.</summary>
    public static string? ModifierValueToken(string key, string display)
    {
        foreach (var (d, token) in ModifierValues(key))
            if (d == display) return token;
        return null;
    }

    // --- Keys: display names + help ---
    public static string KeyName(string key) => key switch
    {
        WorldGenKeys.NoBuildCost => Strings.WorldGen_NoBuildCost_Name,
        WorldGenKeys.PlayerEvents => Strings.WorldGen_PlayerEvents_Name,
        WorldGenKeys.PassiveMobs => Strings.WorldGen_PassiveMobs_Name,
        WorldGenKeys.NoMap => Strings.WorldGen_NoMap_Name,
        WorldGenKeys.Fire => Strings.WorldGen_Fire_Name,
        _ => key,
    };

    public static string KeyHelp(string key) => key switch
    {
        WorldGenKeys.NoBuildCost => Strings.WorldGen_NoBuildCost_Help,
        WorldGenKeys.PlayerEvents => Strings.WorldGen_PlayerEvents_Help,
        WorldGenKeys.PassiveMobs => Strings.WorldGen_PassiveMobs_Help,
        WorldGenKeys.NoMap => Strings.WorldGen_NoMap_Help,
        WorldGenKeys.Fire => Strings.WorldGen_Fire_Help,
        _ => string.Empty,
    };
}
