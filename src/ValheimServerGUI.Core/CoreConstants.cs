using System;

namespace ValheimServerGUI
{
    /// <summary>
    /// OS-independent constants consumed by the service layer. Replaces the plain-string members of
    /// the WinForms <c>Properties.Resources</c> (<c>Resources.resx</c>) that the service layer read,
    /// turning the parse-from-string sites into typed constants. Path-shaped members (which embed
    /// <c>%ProgramFiles(x86)%</c>/<c>%USERPROFILE%</c> and differ per OS) are NOT here — those are
    /// resolved through <c>IValheimPathResolver</c> instead.
    /// </summary>
    public static class CoreConstants
    {
        /// <summary>Default Valheim dedicated-server port. (Resources: DefaultServerPort "2456".)</summary>
        public const int DefaultServerPort = 2456;

        /// <summary>Default auto-save interval, in seconds. (Resources: DefaultSaveInterval "1800".)</summary>
        public const int DefaultSaveInterval = 1800;

        /// <summary>Default number of world backups to retain. (Resources: DefaultBackupCount "4".)</summary>
        public const int DefaultBackupCount = 4;

        /// <summary>Default short backup interval, in seconds. (Resources: DefaultBackupIntervalShort "7200".)</summary>
        public const int DefaultBackupIntervalShort = 7200;

        /// <summary>Default long backup interval, in seconds. (Resources: DefaultBackupIntervalLong "43200".)</summary>
        public const int DefaultBackupIntervalLong = 43200;

        /// <summary>Name of the default server profile. (Resources: DefaultServerProfileName "Default".)</summary>
        public const string DefaultServerProfileName = "Default";

        /// <summary>
        /// Valheim's Steam App ID. Passed as the <c>SteamAppId</c> environment variable when launching
        /// the server and used as a path segment when locating Steam Cloud saves.
        /// (Resources: ValheimSteamAppId "892970".)
        /// </summary>
        public const string ValheimSteamAppId = "892970";

        /// <summary>How often to check for application updates. (Resources: UpdateCheckInterval "1.00:00:00".)</summary>
        public static readonly TimeSpan UpdateCheckInterval = TimeSpan.FromDays(1);

        /// <summary>GitHub API base for the application repo. (Resources: UrlGithubApi.)</summary>
        public const string UrlGithubApi = "https://api.github.com/repos/runeberry/ValheimServerGUI";

        /// <summary>External IP lookup endpoint. (Resources: UrlExternalIpLookup.)</summary>
        public const string UrlExternalIpLookup = "https://api.ipify.org?format=json";

        /// <summary>Fallback external-IP endpoints tried in order after <see cref="UrlExternalIpLookup"/> (§16.2, E52).</summary>
        public const string UrlExternalIpLookupFallback1 = "https://ifconfig.co/ip";
        public const string UrlExternalIpLookupFallback2 = "https://icanhazip.com";

        /// <summary>
        /// Name-lookup + crash-report backend base (player-info lookups, crash reports). The original
        /// Runeberry AWS endpoint below is <b>dead</b>; its replacement is the Cloudflare Worker in
        /// <c>backend/name-lookup-worker/</c>, which speaks the identical contract. Repoint this to the
        /// deployed Worker URL (e.g. <c>https://vsg-name-lookup.&lt;subdomain&gt;.workers.dev</c>) once it is
        /// live, and set the Worker's <c>CLIENT_API_KEY</c> secret to <c>ClientSecrets.RuneberryClientApiKey</c>.
        /// This is the ONLY client-side wiring change; the client, RestClient, and lookup flow are unchanged.
        /// (Resources: UrlRuneberryApi.)
        /// </summary>
        public const string UrlRuneberryApi = "https://u312zw22d6.execute-api.us-east-1.amazonaws.com/Prod";
    }
}
