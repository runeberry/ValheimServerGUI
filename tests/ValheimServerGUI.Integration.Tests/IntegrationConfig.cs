using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace ValheimServerGUI.Integration.Tests
{
    /// <summary>Which server installation the suite runs against (<c>VSG_IT_SERVER_CONFIG</c>).</summary>
    internal enum ServerConfiguration
    {
        /// <summary>An unmodded server: no mod loader may load.</summary>
        Vanilla,

        /// <summary>A server with BepInExPack_Valheim and <see cref="IntegrationConfig.ExpectedPlugins"/> installed.</summary>
        BepInEx,
    }

    /// <summary>
    /// Configuration for the live server tests, read from environment variables. On Linux,
    /// <c>scripts/integration.sh</c> runs the suite in the containers built from <c>tests/integration-image</c>,
    /// whose images set the server variables; elsewhere the caller sets them.
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

        /// <summary>
        /// The folder for all server output — never a real user's save folder. scripts/integration.sh creates a
        /// fresh one per run; a run without it gets one throwaway temp folder.
        /// </summary>
        public string SaveDir { get; }

        /// <summary>Folder for run artifacts (the captured server log).</summary>
        public string ArtifactDir { get; }

        /// <summary>Which installation <see cref="ServerExe"/> belongs to; decides what the mod-loader test expects.</summary>
        public ServerConfiguration ServerConfig { get; }

        /// <summary>
        /// For <see cref="ServerConfiguration.BepInEx"/>: the installed plugins as the BepInEx chainloader names them
        /// when loading them (<c>"&lt;Name&gt; &lt;Version&gt;"</c>). Empty for a vanilla server.
        /// </summary>
        public IReadOnlyList<string> ExpectedPlugins { get; }

        /// <summary>
        /// The BepInEx preloader and Doorstop library paths the suite configures, as the app's Set Directories
        /// would. Linux only (Windows ignores them, so they are null there): the bepinex images point them at the
        /// installed pack, the vanilla image at where the pack would be.
        /// </summary>
        public string? BepInExPreloader { get; }

        /// <inheritdoc cref="BepInExPreloader"/>
        public string? DoorstopLibrary { get; }

        private IntegrationConfig(string serverExe, string saveDir, string artifactDir,
            ServerConfiguration serverConfig, IReadOnlyList<string> expectedPlugins, string? bepInExPreloader, string? doorstopLibrary)
        {
            BepInExPreloader = bepInExPreloader;
            DoorstopLibrary = doorstopLibrary;
            ServerExe = serverExe;
            SaveDir = saveDir;
            ArtifactDir = artifactDir;
            ServerConfig = serverConfig;
            ExpectedPlugins = expectedPlugins;
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

            var configName = Environment.GetEnvironmentVariable("VSG_IT_SERVER_CONFIG");
            var serverConfig = ServerConfiguration.Vanilla;
            if (string.IsNullOrWhiteSpace(configName))
                problems.Add("VSG_IT_SERVER_CONFIG is not set (vanilla or bepinex: which installation VSG_IT_SERVER_EXE is).");
            else if (!Enum.TryParse(configName, ignoreCase: true, out serverConfig) || !Enum.IsDefined(serverConfig))
                problems.Add($"VSG_IT_SERVER_CONFIG must be vanilla or bepinex, not '{configName}'.");

            var expectedPlugins = (Environment.GetEnvironmentVariable("VSG_IT_EXPECTED_PLUGINS") ?? string.Empty)
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (serverConfig == ServerConfiguration.BepInEx && expectedPlugins.Length == 0)
                problems.Add("VSG_IT_EXPECTED_PLUGINS is not set (the installed plugins as BepInEx logs them, ';'-separated, e.g. 'Server Devcommands 1.115;Network 1.0.2').");
            else if (serverConfig == ServerConfiguration.Vanilla && expectedPlugins.Length > 0)
                problems.Add($"VSG_IT_EXPECTED_PLUGINS is set to '{string.Join(';', expectedPlugins)}', but a vanilla server has no plugins.");

            string? bepInExPreloader = null, doorstopLibrary = null;
            if (!OperatingSystem.IsWindows())
            {
                bepInExPreloader = Environment.GetEnvironmentVariable("VSG_IT_BEPINEX_PRELOADER");
                doorstopLibrary = Environment.GetEnvironmentVariable("VSG_IT_DOORSTOP_LIBRARY");
                if (string.IsNullOrWhiteSpace(bepInExPreloader))
                    problems.Add("VSG_IT_BEPINEX_PRELOADER is not set (the BepInEx.Preloader.dll path to configure).");
                if (string.IsNullOrWhiteSpace(doorstopLibrary))
                    problems.Add("VSG_IT_DOORSTOP_LIBRARY is not set (the libdoorstop_x64.so path to configure).");
            }

            if (problems.Count > 0)
            {
                throw new InvalidOperationException(
                    "The integration tests are misconfigured. On Linux, run them with scripts/integration.sh, whose " +
                    "server images set these variables; elsewhere, set them before running the tests:" +
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

            return new IntegrationConfig(serverExe!, saveDir, artifactDir, serverConfig, expectedPlugins,
                bepInExPreloader, doorstopLibrary);
        }
    }
}
