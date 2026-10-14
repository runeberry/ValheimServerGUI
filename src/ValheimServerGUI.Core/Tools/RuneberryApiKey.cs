using ValheimServerGUI.Properties;

namespace ValheimServerGUI.Tools
{
    /// <summary>The request header, and its value, that authenticate this client to the Runeberry API.</summary>
    public sealed record RuneberryApiKey(string Header, string Value)
    {
        /// <summary>
        /// The key compiled into this build. Both parts are empty in a build without the secret values file, in
        /// which case no key header is sent and the API refuses the request.
        /// </summary>
        public static RuneberryApiKey FromClientSecrets()
            => new(ClientSecrets.RuneberryApiKeyHeader, ClientSecrets.RuneberryClientApiKey);
    }
}
