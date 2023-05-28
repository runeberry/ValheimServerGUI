using System.Collections.Generic;

namespace ValheimServerGUI.Tools.Models
{
    public static class PlayerPlatforms
    {
        public const string Steam = "Steam";
        public const string Xbox = "Xbox";

        public static readonly HashSet<string> All = new()
        {
            Steam,
            Xbox,
        };

        /// <summary>
        /// Gets a case-corrected platform name from an input string.
        /// </summary>
        public static bool TryGetValidPlatform(string input, out string platform)
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
                default:
                    platform = null;
                    return false;
            }
        }
    }
}
