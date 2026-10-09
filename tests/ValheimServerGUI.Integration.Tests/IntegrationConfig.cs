using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace ValheimServerGUI.Integration.Tests
{
    /// <summary>
    /// Machine-specific configuration for the live tier-4 smoke, read from environment variables that
    /// <c>scripts/integration.sh</c> exports (sourced from the gitignored <c>scripts/integration.local.env</c>).
    ///
    /// <para>These tests only run when asked for explicitly (<c>dotnet test -p:Integration=true</c>; see the
    /// csproj), so a run that reaches them means "test against a real server". The configuration is therefore
    /// validated once, here, and any problem is a failure: <see cref="Current"/> throws one error listing every
    /// misconfiguration, and every test that uses it fails with that message. Nothing is skipped.</para>
    /// </summary>
    internal sealed class IntegrationConfig
    {
        private static readonly Lazy<IntegrationConfig> Loaded = new(Load, LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>The validated configuration. Throws (on every access) if it is misconfigured.</summary>
        public static IntegrationConfig Current => Loaded.Value;

        /// <summary>Full path to the real Valheim dedicated-server binary (appid 896660).</summary>
        public string ServerExe { get; }

        /// <summary>Steam install root containing <c>userdata</c> (for the cloud-import round-trip).</summary>
        public string SteamRoot { get; }

        /// <summary>
        /// The folder for all server output — never Nuffle's real save folder. scripts/integration.sh creates a
        /// fresh one per run; a run without it gets one throwaway temp folder.
        /// </summary>
        public string SaveDir { get; }

        /// <summary>Folder for run artifacts (the captured server log).</summary>
        public string ArtifactDir { get; }

        private IntegrationConfig(string serverExe, string steamRoot, string saveDir, string artifactDir)
        {
            ServerExe = serverExe;
            SteamRoot = steamRoot;
            SaveDir = saveDir;
            ArtifactDir = artifactDir;
        }

        private static IntegrationConfig Load()
        {
            var problems = new List<string>();

            var serverExe = Environment.GetEnvironmentVariable("VSG_IT_SERVER_EXE");
            if (string.IsNullOrWhiteSpace(serverExe))
                problems.Add("VSG_IT_SERVER_EXE is not set (the path to the Valheim dedicated server binary).");
            else if (!File.Exists(serverExe))
                problems.Add($"VSG_IT_SERVER_EXE does not exist: {serverExe}");
            else if (!OperatingSystem.IsWindows() && !File.GetUnixFileMode(serverExe).HasFlag(UnixFileMode.UserExecute))
                problems.Add($"VSG_IT_SERVER_EXE is not executable: {serverExe}");

            var steamRoot = Environment.GetEnvironmentVariable("VSG_IT_STEAM_ROOT");
            if (string.IsNullOrWhiteSpace(steamRoot))
                problems.Add("VSG_IT_STEAM_ROOT is not set (the Steam install folder that contains \"userdata\").");
            else if (!Directory.Exists(Path.Join(steamRoot, "userdata")))
                problems.Add($"VSG_IT_STEAM_ROOT has no userdata folder: {steamRoot}");

            if (problems.Count > 0)
            {
                throw new InvalidOperationException(
                    "The integration tests are misconfigured. Run them with scripts/integration.sh, which loads " +
                    "scripts/integration.local.env (see scripts/integration.local.env.example):" +
                    Environment.NewLine + "  - " + string.Join(Environment.NewLine + "  - ", problems));
            }

            var saveDir = Environment.GetEnvironmentVariable("VSG_IT_SAVEDIR") is { Length: > 0 } dir
                ? dir
                : Path.Join(Path.GetTempPath(), "vsg-it-savedir-" + Guid.NewGuid().ToString("n"));
            var artifactDir = Environment.GetEnvironmentVariable("VSG_IT_ARTIFACT_DIR") is { Length: > 0 } artifacts
                ? artifacts
                : Path.Join(Path.GetTempPath(), "vsg-it-artifacts");
            Directory.CreateDirectory(saveDir);
            Directory.CreateDirectory(artifactDir);

            return new IntegrationConfig(serverExe!, steamRoot!, saveDir, artifactDir);
        }
    }
}
