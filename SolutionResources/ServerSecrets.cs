using System.Collections.Generic;

namespace ValheimServerGUI.Properties
{
    /// <summary>
    /// The values in this class are populated by the static constructor in the corresponding
    /// partial class, which is kept out of source control.
    /// </summary>
    public static partial class ServerSecrets
    {
        /// <summary>
        /// The HTTP header that will contain the API key for requests made to the Runeberry API.
        /// </summary>
        public static string RuneberryApiKeyHeader { get; } = string.Empty;

        /// <summary>
        /// The Runeberry API keys that will be accepted by the server.
        /// </summary>
        public static HashSet<string> RuneberryServerApiKeys { get; } = new();

        /// <summary>
        /// API key for interacting with the Steamworks Web API.
        /// </summary>
        public static string SteamApiKey { get; }

        /// <summary>
        /// API key for interacting with the OpenXBL API.
        /// </summary>
        public static string XboxApiKey { get; }
    }
}
