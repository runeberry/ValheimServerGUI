using System;
using System.IO;
using Xunit;

namespace ValheimServerGUI.Integration.Tests
{
    /// <summary>
    /// Machine-specific configuration for the live tier-4 smoke, read from environment variables that
    /// <c>scripts/integration.sh</c> exports (sourced from the gitignored <c>scripts/integration.local.env</c>).
    /// Every live test calls one of the <c>SkipIf*</c> guards first, so a direct <c>dotnet test</c> with no
    /// config no-ops (skips) instead of trying to launch a server.
    /// </summary>
    internal static class IntegrationConfig
    {
        /// <summary>Full path to the real Valheim dedicated-server binary (appid 896660).</summary>
        public static string? ServerExe => Environment.GetEnvironmentVariable("VSG_IT_SERVER_EXE");

        /// <summary>Steam install root containing <c>userdata</c> (for the cloud-import round-trip).</summary>
        public static string? SteamRoot => Environment.GetEnvironmentVariable("VSG_IT_STEAM_ROOT");

        /// <summary>
        /// A mochi-owned temp savedir for all server output (never Nuffle's real save folder).
        /// scripts/integration.sh creates and exports this; if unset (direct run), a throwaway temp dir
        /// is used so isolation holds either way.
        /// </summary>
        public static string SaveDir =>
            Environment.GetEnvironmentVariable("VSG_IT_SAVEDIR") is { Length: > 0 } dir
                ? dir
                : EnsureDir(Path.Join(Path.GetTempPath(), "vsg-it-savedir-" + Guid.NewGuid().ToString("n")));

        /// <summary>Directory run artifacts (captured server log) are written to.</summary>
        public static string ArtifactDir =>
            Environment.GetEnvironmentVariable("VSG_IT_ARTIFACT_DIR") is { Length: > 0 } dir
                ? EnsureDir(dir)
                : EnsureDir(Path.Join(Path.GetTempPath(), "vsg-it-artifacts"));

        /// <summary>Skips the calling test unless a real dedicated-server binary is configured and present.</summary>
        public static void SkipIfServerUnconfigured()
        {
            Assert.SkipUnless(
                !string.IsNullOrWhiteSpace(ServerExe) && File.Exists(ServerExe),
                "VSG_IT_SERVER_EXE is unset or missing — run via scripts/integration.sh (see scripts/integration.local.env.example). Live server smoke skipped.");
        }

        /// <summary>Skips the calling test unless a Steam install root with userdata is configured and present.</summary>
        public static void SkipIfSteamUnconfigured()
        {
            Assert.SkipUnless(
                !string.IsNullOrWhiteSpace(SteamRoot) && Directory.Exists(Path.Join(SteamRoot, "userdata")),
                "VSG_IT_STEAM_ROOT is unset or has no userdata — run via scripts/integration.sh. Cloud-import smoke skipped.");
        }

        private static string EnsureDir(string path)
        {
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
