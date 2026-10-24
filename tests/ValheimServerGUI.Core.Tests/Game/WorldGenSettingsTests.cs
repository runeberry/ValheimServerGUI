using System;
using System.Collections.Generic;
using System.Linq;
using ValheimServerGUI.Game;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Game;

/// <summary>
/// World settings as the game stores them: the preset table, the game-style preset name, reading stored keys back
/// into settings, and which stored settings a launch would change.
/// </summary>
public class WorldGenSettingsTests
{
    private static WorldGenSettings Settings(Dictionary<string, string>? modifiers = null, params string[] keys)
        => new(modifiers, keys);

    private static WorldGenSettings Preset(string token) => WorldGenPresets.Find(token)!.Settings;

    // The keys of each preset button in the game's start scene (Valheim 1.0.20), as the game stores them.
    public static TheoryData<string, string[]> GamePresetKeys => new()
    {
        { WorldGenPresets.Easy, new[] { "playerdamage 110", "enemydamage 75", "eventrate 150", "EnemySpeedSize 95" } },
        { WorldGenPresets.Hard, new[] { "playerdamage 85", "enemydamage 150", "eventrate 60", "EnemySpeedSize 110", "enemyleveluprate 120" } },
        { WorldGenPresets.Hardcore, new[] { "playerdamage 70", "enemydamage 200", "eventrate 60", "deathskillsreset", "deathdeleteitems", "nomap", "nobossportals", "enemyspeedsize 120", "enemyleveluprate 140" } },
        { WorldGenPresets.Casual, new[] { "passivemobs", "eventrate 0", "resourcerate 150", "playerdamage 125", "enemydamage 50", "deathkeepequip", "skillreductionrate 15", "teleportall", "playerevents", "EnemySpeedSize 90" } },
        { WorldGenPresets.Hammer, new[] { "nobuildcost", "passivemobs", "eventrate 0" } },
        { WorldGenPresets.Immersive, new[] { "nomap", "noportals", "fire" } },
        { WorldGenPresets.Normal, Array.Empty<string>() },
    };

    [Theory]
    [MemberData(nameof(GamePresetKeys))]
    public void Preset_stores_exactly_the_game_preset_keys(string preset, string[] gameKeys)
    {
        var expected = gameKeys.Select(k => k.ToLowerInvariant()).OrderBy(k => k);

        Assert.Equal(expected, Preset(preset).ToGameKeys().OrderBy(k => k));
    }

    public static TheoryData<string, WorldGenSettings, string?, bool> Labels => new()
    {
        { "all normal", Settings(), WorldGenPresets.Normal, false },
        { "exactly Easy", Preset(WorldGenPresets.Easy), WorldGenPresets.Easy, false },
        { "exactly Hard", Preset(WorldGenPresets.Hard), WorldGenPresets.Hard, false },
        { "exactly Hardcore", Preset(WorldGenPresets.Hardcore), WorldGenPresets.Hardcore, false },
        { "exactly Casual", Preset(WorldGenPresets.Casual), WorldGenPresets.Casual, false },
        { "exactly Hammer", Preset(WorldGenPresets.Hammer), WorldGenPresets.Hammer, false },
        { "exactly Immersive", Preset(WorldGenPresets.Immersive), WorldGenPresets.Immersive, false },
        {
            "Hard plus a death penalty",
            Settings(new() { ["combat"] = "hard", ["raids"] = "more", ["deathpenalty"] = "easy" }),
            WorldGenPresets.Hard, true
        },
        { "Casual plus a key", Settings(Preset(WorldGenPresets.Casual).Modifiers.ToDictionary(), "passivemobs", "playerevents", "nomap"), WorldGenPresets.Casual, true },
        // Hard has 5 keys and Immersive 3, so Hard is named though Immersive sets more fields.
        {
            "Hard and Immersive together",
            Settings(new() { ["combat"] = "hard", ["raids"] = "more", ["portals"] = "veryhard" }, "nomap", "fire"),
            WorldGenPresets.Hard, true
        },
        // Hammer and Immersive both have 3 keys; the earlier button (Hammer) wins the tie.
        {
            "Hammer and Immersive together",
            Settings(new() { ["raids"] = "none", ["portals"] = "veryhard" }, "nobuildcost", "passivemobs", "nomap", "fire"),
            WorldGenPresets.Hammer, true
        },
        { "one modifier only", Settings(new() { ["combat"] = "hard" }), null, true },
        { "Hardcore without No Map", Settings(Preset(WorldGenPresets.Hardcore).Modifiers.ToDictionary()), null, true },
    };

    [Theory]
    [MemberData(nameof(Labels))]
    public void Label_names_settings_like_the_game(string _, WorldGenSettings settings, string? preset, bool hasExtras)
    {
        Assert.Equal(new WorldGenPresetLabel(preset, hasExtras), WorldGenPresets.Label(settings));
    }

    public static TheoryData<string, string> EveryModifierValue()
    {
        var data = new TheoryData<string, string>();
        foreach (var (modifier, values) in WorldGenModifiers.AllowedValues)
            foreach (var value in values) data.Add(modifier, value);
        return data;
    }

    [Theory]
    [MemberData(nameof(EveryModifierValue))]
    public void Every_modifier_value_reads_back_from_its_stored_keys(string modifier, string value)
    {
        var stored = Settings(new() { [modifier] = value }).ToGameKeys();

        var read = WorldGenSettings.FromGameKeys(stored, out var unrecognized);

        Assert.Equal(new Dictionary<string, string> { [modifier] = value }, read.Modifiers);
        Assert.Empty(unrecognized);
    }

    [Fact]
    public void Every_preset_reads_back_from_its_stored_keys()
    {
        foreach (var preset in WorldGenPresets.All)
        {
            var read = WorldGenSettings.FromGameKeys(preset.Settings.ToGameKeys(), out var unrecognized);

            Assert.Equal(preset.Settings.Modifiers, read.Modifiers);
            Assert.Equal(preset.Settings.Keys.OrderBy(k => k), read.Keys.OrderBy(k => k));
            Assert.Empty(unrecognized);
        }
    }

    // Death Casual stores Very Easy's key plus one more; both present means Casual.
    [Fact]
    public void Reading_prefers_the_value_with_the_most_keys()
    {
        var read = WorldGenSettings.FromGameKeys(new[] { "skillreductionrate 15", "deathkeepequip" }, out _);

        Assert.Equal("casual", read.Modifiers["deathpenalty"]);
    }

    [Fact]
    public void Reading_reports_keys_no_setting_accounts_for_and_ignores_the_preset_marker()
    {
        var read = WorldGenSettings.FromGameKeys(
            new[] { "PlayerDamage 137", "nocraftcost", "preset combat_hard", "nomap" }, out var unrecognized);

        Assert.Empty(read.Modifiers);
        Assert.Equal(new[] { "nomap" }, read.Keys);
        Assert.Equal(new[] { "PlayerDamage 137", "nocraftcost" }, unrecognized);
    }

    // Keys VSG doesn't own (dungeonbuild) are carried over, so they are not changes; a custom value of a key VSG does
    // own (eventrate, set by Raids) is replaced, so it is.
    [Fact]
    public void Changes_list_stored_settings_the_launch_changes_or_replaces()
    {
        var stored = Settings(new() { ["combat"] = "hard", ["portals"] = "casual" }, "nomap", "fire")
            .ToGameKeys().Append("eventrate 42").Append("dungeonbuild").Append("preset combat_hard");
        var target = Settings(new() { ["combat"] = "veryhard", ["resources"] = "more" }, "fire", "passivemobs");

        var changes = target.ChangesFrom(stored);

        Assert.Equal(
            new[]
            {
                new WorldGenChange(WorldGenChangeKind.Modifier, "combat", "hard", "veryhard"),
                new WorldGenChange(WorldGenChangeKind.Modifier, "portals", "casual", null),
                new WorldGenChange(WorldGenChangeKind.Key, "nomap"),
                new WorldGenChange(WorldGenChangeKind.CustomValue, "eventrate 42"),
            },
            changes);
    }

    // Console keys, mod keys and anything a newer game adds are the world's own; VSG's keys and the preset marker
    // (which the game re-derives) are not carried over.
    [Fact]
    public void Keys_vsg_does_not_own_are_everything_its_settings_cannot_write()
    {
        var stored = Preset(WorldGenPresets.Casual).ToGameKeys()
            .Concat(new[] { "PlayerDamage 137", "nocraftcost", "NoWorkbench", "somefuturekey 50", "preset casual", "nocraftcost" });

        Assert.Equal(
            new[] { "nocraftcost", "NoWorkbench", "somefuturekey 50" },
            WorldGenSettings.KeysVsgDoesNotOwn(stored));
    }

    // A Normal world only gains settings: nothing to confirm.
    [Fact]
    public void Changes_are_empty_when_the_launch_only_adds_settings()
    {
        Assert.Empty(Preset(WorldGenPresets.Hardcore).ChangesFrom(Array.Empty<string>()));
    }

    // Very Easy's keys are a subset of Casual's, but moving Very Easy to Casual still changes a stored setting.
    [Fact]
    public void Changes_include_a_value_whose_keys_the_new_value_contains()
    {
        var stored = Settings(new() { ["deathpenalty"] = "veryeasy" }).ToGameKeys();

        var changes = Settings(new() { ["deathpenalty"] = "casual" }).ChangesFrom(stored);

        Assert.Equal(new[] { new WorldGenChange(WorldGenChangeKind.Modifier, "deathpenalty", "veryeasy", "casual") }, changes);
    }

    [Fact]
    public void Changes_are_empty_when_the_world_already_matches()
    {
        var settings = Preset(WorldGenPresets.Casual);

        Assert.Empty(settings.ChangesFrom(settings.ToGameKeys().Append("preset casual")));
    }
}
