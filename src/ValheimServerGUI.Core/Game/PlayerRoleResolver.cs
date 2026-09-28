using System.Collections.Generic;
using System.Linq;

namespace ValheimServerGUI.Game
{
    /// <summary>A player's resolved role on one server, plus where it came from.</summary>
    /// <param name="Effective">The role that applies: override ?? default ?? <see cref="PlayerRole.None"/>.</param>
    /// <param name="HasOverride">True when the server profile stores an override for the player.</param>
    /// <param name="HasDefault">True when the player sits on a Manage Players list (has a global default).</param>
    public readonly record struct ResolvedRole(PlayerRole Effective, bool HasOverride, bool HasDefault)
    {
        /// <summary>True when a server override replaces a global default (the Players tab's <c>(*)</c> marker).
        /// An override equal to the default still counts: it pins the role against future default changes.</summary>
        public bool ShowsOverrideMarker => HasOverride && HasDefault;
    }

    /// <summary>
    /// The one place a player's effective role on a server is computed: the server profile's override wins,
    /// then the player's app-global default, then <see cref="PlayerRole.None"/>.
    /// </summary>
    public static class PlayerRoleResolver
    {
        public static ResolvedRole Resolve(
            string key,
            IReadOnlyDictionary<string, PlayerRoleEntry> overrides,
            IReadOnlyDictionary<string, PlayerDefaultEntry> defaults)
        {
            var hasOverride = overrides.TryGetValue(key, out var over);
            var hasDefault = defaults.TryGetValue(key, out var def);
            var effective = hasOverride ? over!.Role : hasDefault ? def!.DefaultRole : PlayerRole.None;
            return new ResolvedRole(effective, hasOverride, hasDefault);
        }

        /// <summary>
        /// The resolved role of every player with an override or a default, keyed by <c>Platform:PlayerId</c>,
        /// skipping effective <see cref="PlayerRole.None"/>. PlatformRaw comes from the override when present,
        /// else the default.
        /// </summary>
        public static Dictionary<string, PlayerRoleEntry> EffectiveRoles(
            IReadOnlyDictionary<string, PlayerRoleEntry> overrides,
            IReadOnlyDictionary<string, PlayerDefaultEntry> defaults)
        {
            var result = new Dictionary<string, PlayerRoleEntry>();
            foreach (var key in overrides.Keys.Union(defaults.Keys))
            {
                var resolved = Resolve(key, overrides, defaults);
                if (resolved.Effective == PlayerRole.None) continue;

                var platformRaw = overrides.TryGetValue(key, out var over) ? over.PlatformRaw : defaults[key].PlatformRaw;
                result[key] = new PlayerRoleEntry(resolved.Effective, platformRaw);
            }
            return result;
        }

        /// <summary>
        /// The self-contained assignments the list-file generator consumes (no repo dependency): every player's
        /// effective role, splitting the <c>"{Platform}:{PlayerId}"</c> key.
        /// </summary>
        public static IReadOnlyList<PlayerRoleAssignment> BuildAssignments(
            IReadOnlyDictionary<string, PlayerRoleEntry> overrides,
            IReadOnlyDictionary<string, PlayerDefaultEntry> defaults)
            => EffectiveRoles(overrides, defaults)
                .OrderBy(kvp => kvp.Key, System.StringComparer.Ordinal)
                .Select(kvp =>
                {
                    var separator = kvp.Key.IndexOf(':');
                    var platform = separator >= 0 ? kvp.Key[..separator] : null;
                    var playerId = separator >= 0 ? kvp.Key[(separator + 1)..] : kvp.Key;
                    return new PlayerRoleAssignment(platform, kvp.Value.PlatformRaw ?? platform, playerId, kvp.Value.Role);
                })
                .ToList();
    }
}
