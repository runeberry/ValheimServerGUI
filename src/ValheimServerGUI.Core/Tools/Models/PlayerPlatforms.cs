using System.Collections.Generic;

namespace ValheimServerGUI.Tools.Models
{
    public static class PlayerPlatforms
    {
        public const string Steam = "Steam";
        public const string Xbox = "Xbox";
        public const string PlayStation = "PlayStation";
        public const string Nintendo = "Nintendo";

        public static readonly HashSet<string> All = new()
        {
            Steam,
            Xbox,
            PlayStation,
            Nintendo,
        };

        /// <summary>
        /// Gets a case-corrected platform name from an input string. The accepted inputs are the tokens the
        /// game prints in its crossplay log lines (case-insensitively), mapped to VSG's canonical name used
        /// for UI/icons and the name-lookup API. The raw log token is preserved separately on
        /// <see cref="ValheimServerGUI.Game.PlayerInfo.PlatformRaw"/> for writing list-file entries, so
        /// this normalization never has to guess the binary's exact casing.
        /// </summary>
        public static bool TryGetValidPlatform(string? input, out string? platform)
        {
            if (input == null)
            {
                platform = null;
                return false;
            }

            switch (input.Trim().ToLowerInvariant())
            {
                case "steam":
                    platform = Steam;
                    return true;
                case "xbox":
                    platform = Xbox;
                    return true;
                case "playstation":
                    platform = PlayStation;
                    return true;
                case "nintendo":
                case "switch":
                    platform = Nintendo;
                    return true;
                default:
                    platform = null;
                    return false;
            }
        }
    }
}
