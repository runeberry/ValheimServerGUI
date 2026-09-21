using System;
using System.Text.RegularExpressions;
using ValheimServerGUI.Tools.Models;

namespace ValheimServerGUI.Game
{
    /// <summary>
    /// Resolves a raw list-file token (one non-comment line of <c>adminlist.txt</c> / <c>bannedlist.txt</c> /
    /// <c>permittedlist.txt</c>) back to a VSG player identity. This is the inverse of
    /// <see cref="PlayerAccessListService"/>'s canonical-write form and is the ONLY place tokens are parsed
    /// into a platform, so the heuristic lives in one spot.
    ///
    /// <para>The resolution is deliberately <b>fail-closed</b> (a caller fails the whole import on any
    /// unresolved token) because deeper study of the binary's exact token grammar is deferred:</para>
    /// <list type="bullet">
    /// <item>A prefixed <c>&lt;Platform&gt;_&lt;id&gt;</c> token resolves when the prefix is a known platform
    /// (case-insensitively, via <see cref="PlayerPlatforms.TryGetValidPlatform"/>). The remainder is the id;
    /// the raw prefix is preserved verbatim for case-exact non-Steam re-writes, except Steam which normalizes
    /// to <c>"Steam"</c>.</item>
    /// <item>A bare, all-digit, steam64-shaped token (<c>7656119XXXXXXXXXX</c>) resolves to Steam.</item>
    /// <item>Anything else (unknown prefix, bare non-steam64 digits, an id carrying a further <c>_</c>, empty)
    /// fails.</item>
    /// </list>
    /// </summary>
    public static class PlayerListToken
    {
        // A bare Steam64 id: the "7656119" community-id prefix followed by 10 more digits (17 total). Only a
        // token of exactly this shape is treated as a prefix-less Steam id; any other bare token fails.
        private static readonly Regex Steam64Regex = new(@"^7656119\d{10}$", RegexOptions.Compiled);

        /// <summary>
        /// Attempts to resolve <paramref name="token"/> to a platform identity. Returns false (and empty outs)
        /// when the token cannot be resolved with confidence.
        /// </summary>
        /// <param name="platform">VSG's normalized platform name (e.g. "Steam", "Xbox", "PlayStation").</param>
        /// <param name="platformRaw">The token to re-write list entries with: the exact prefix for non-Steam,
        /// always "Steam" for Steam.</param>
        /// <param name="playerId">The platform id portion.</param>
        public static bool TryResolve(string? token, out string platform, out string platformRaw, out string playerId)
        {
            platform = string.Empty;
            platformRaw = string.Empty;
            playerId = string.Empty;

            if (string.IsNullOrWhiteSpace(token)) return false;
            token = token.Trim();

            var underscore = token.IndexOf('_');
            if (underscore >= 0)
            {
                var prefix = token[..underscore];
                var remainder = token[(underscore + 1)..];

                // A real platform token is <Platform>_<id> with a single separator; an id carrying a further
                // '_' (or an empty id) is malformed and fails closed.
                if (remainder.Length == 0 || remainder.Contains('_')) return false;
                if (!PlayerPlatforms.TryGetValidPlatform(prefix, out var normalized) || normalized is null) return false;

                platform = normalized;
                // Steam always writes canonically as "Steam"; other platforms preserve the exact prefix casing.
                platformRaw = normalized == PlayerPlatforms.Steam ? PlayerPlatforms.Steam : prefix;
                playerId = remainder;
                return true;
            }

            // Bare token: only a steam64-shaped all-digit id resolves (to Steam).
            if (Steam64Regex.IsMatch(token))
            {
                platform = PlayerPlatforms.Steam;
                platformRaw = PlayerPlatforms.Steam;
                playerId = token;
                return true;
            }

            return false;
        }
    }
}
