namespace ValheimServerGUI.Properties
{
    /// <summary>
    /// The values in this class are populated by the static constructor in the corresponding
    /// partial class, which is kept out of source control.
    /// </summary>
    public static partial class ClientSecrets
    {
        /// <summary>
        /// The HTTP header that will contain the API key for requests made to the Runeberry API.
        /// </summary>
        public static string RuneberryApiKeyHeader { get; } = string.Empty;

        /// <summary>
        /// The API key that will be attached to all VSG client requests to the Runeberry API.
        /// </summary>
        public static string RuneberryClientApiKey { get; }
    }
}
