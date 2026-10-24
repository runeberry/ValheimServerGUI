using System;
using System.Collections.Generic;
using System.Linq;

namespace ValheimServerGUI.Game
{
    /// <summary>What kind of world setting a <see cref="WorldGenChange"/> is about.</summary>
    public enum WorldGenChangeKind
    {
        /// <summary>A modifier (<see cref="WorldGenModifiers"/>); values are modifier value tokens, null = Normal.</summary>
        Modifier,

        /// <summary>A world key (<see cref="WorldGenKeys"/>); the world has it set and the new settings do not.</summary>
        Key,

        /// <summary>A stored key a VSG setting controls, holding a value no setting expresses (e.g. a console-set
        /// <c>playerdamage 137</c>); the launch replaces it.</summary>
        CustomValue,
    }

    /// <summary>One world setting that starting the server with new settings would change or remove.</summary>
    /// <param name="Name">The modifier token, the world key token, or the raw stored key (CustomValue).</param>
    /// <param name="From">The world's current modifier value (Modifier only).</param>
    /// <param name="To">The new modifier value, or null for Normal (Modifier only).</param>
    public record WorldGenChange(WorldGenChangeKind Kind, string Name, string? From = null, string? To = null);

    /// <summary>
    /// A world's modifier and key settings: one value per modifier (absent = Normal) and the set world keys. These are
    /// exactly what World Preferences shows, and the server is launched to match them.
    ///
    /// <para>VSG owns only the stored keys its settings can write (by key name, e.g. <c>playerdamage</c>, <c>nomap</c>).
    /// Every other stored key, from a console command, a mod or a newer game version, is the world's own: a launch
    /// carries it over unchanged (<see cref="KeysVsgDoesNotOwn"/>) and never asks about it.</para>
    /// </summary>
    public sealed class WorldGenSettings
    {
        // The game also stores a "preset <name>" key recording how the modifiers were chosen. The game re-derives it
        // from the other keys on every launch, so it is neither compared nor carried over.
        private const string PresetMarkerKey = "preset";

        private static readonly HashSet<string> OwnedKeyNames = WorldGenModifiers.Settings.Values
            .SelectMany(values => values.SelectMany(v => v.GameKeys))
            .Concat(WorldGenKeys.All)
            .Select(KeyName)
            .Append(PresetMarkerKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        public WorldGenSettings(IReadOnlyDictionary<string, string>? modifiers = null, IEnumerable<string>? keys = null)
        {
            Modifiers = new Dictionary<string, string>(modifiers ?? new Dictionary<string, string>());
            Keys = new HashSet<string>(keys ?? Array.Empty<string>());
        }

        public IReadOnlyDictionary<string, string> Modifiers { get; }

        public IReadOnlySet<string> Keys { get; }

        public bool IsDefault => Modifiers.Count == 0 && Keys.Count == 0;

        /// <summary>The world keys the game stores for these settings, lowercase.</summary>
        public IReadOnlySet<string> ToGameKeys()
        {
            var gameKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (modifier, value) in Modifiers) gameKeys.UnionWith(WorldGenModifiers.GameKeys(modifier, value));
            foreach (var key in Keys) gameKeys.Add(key);
            return gameKeys;
        }

        /// <summary>
        /// Reads stored world keys back into settings, the way the game's World Modifiers screen does: for each
        /// modifier, the value with the most keys whose keys are all present (earlier wins a tie), else Normal. Keys
        /// that no setting accounts for are returned as <paramref name="unrecognized"/>.
        /// </summary>
        public static WorldGenSettings FromGameKeys(IEnumerable<string> gameKeys, out IReadOnlyList<string> unrecognized)
        {
            var stored = gameKeys.Where(k => !IsPresetMarker(k)).ToList();
            var present = new HashSet<string>(stored, StringComparer.OrdinalIgnoreCase);
            var accounted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var modifiers = new Dictionary<string, string>();
            foreach (var (modifier, settings) in WorldGenModifiers.Settings)
            {
                var match = settings
                    .Where(s => s.GameKeys.All(present.Contains))
                    .OrderByDescending(s => s.GameKeys.Length)
                    .FirstOrDefault();
                if (match.Value is null) continue;

                modifiers[modifier] = match.Value;
                accounted.UnionWith(match.GameKeys);
            }

            var keys = WorldGenKeys.All.Where(present.Contains).ToList();
            accounted.UnionWith(keys);

            unrecognized = stored.Where(k => !accounted.Contains(k)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            return new WorldGenSettings(modifiers, keys);
        }

        /// <summary>The stored keys no VSG setting controls, which a launch must carry over unchanged.</summary>
        public static IReadOnlyList<string> KeysVsgDoesNotOwn(IEnumerable<string> storedKeys)
            => storedKeys.Where(k => !OwnedKeyNames.Contains(KeyName(k))).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        /// <summary>
        /// The world's existing settings that launching with these settings would change or remove. Turning a Normal
        /// modifier or an unset key ON only adds to the world, so it is not a change.
        /// </summary>
        public IReadOnlyList<WorldGenChange> ChangesFrom(IEnumerable<string> worldGameKeys)
        {
            var world = FromGameKeys(worldGameKeys, out var unrecognized);
            var changes = new List<WorldGenChange>();

            foreach (var modifier in WorldGenModifiers.All)
            {
                if (!world.Modifiers.TryGetValue(modifier, out var from)) continue;
                Modifiers.TryGetValue(modifier, out var to);
                if (from != to) changes.Add(new WorldGenChange(WorldGenChangeKind.Modifier, modifier, from, to));
            }

            foreach (var key in WorldGenKeys.All)
            {
                if (world.Keys.Contains(key) && !Keys.Contains(key)) changes.Add(new WorldGenChange(WorldGenChangeKind.Key, key));
            }

            changes.AddRange(unrecognized
                .Where(k => OwnedKeyNames.Contains(KeyName(k)))
                .Select(k => new WorldGenChange(WorldGenChangeKind.CustomValue, k)));
            return changes;
        }

        private static bool IsPresetMarker(string gameKey) => KeyName(gameKey).Equals(PresetMarkerKey, StringComparison.OrdinalIgnoreCase);

        // A stored key is "<name>" or "<name> <value>".
        private static string KeyName(string gameKey) => gameKey.Split(' ')[0];
    }
}
