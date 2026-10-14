using System.IO;

namespace ValheimServerGUI.Game
{
    /// <summary>
    /// Resolves the OS-specific, path-shaped values the service layer used to read from the WinForms
    /// <c>Properties.Resources</c> (which embedded <c>%ProgramFiles(x86)%</c>/<c>%USERPROFILE%</c> and
    /// do not port). The Windows implementation reproduces the paths existing installs already use;
    /// the Linux implementation uses XDG / <c>$HOME</c> conventions.
    /// </summary>
    public interface IValheimPathResolver
    {
        /// <summary>The Valheim dedicated-server executable name for this OS
        /// (<c>valheim_server.exe</c> / <c>valheim_server.x86_64</c>).</summary>
        string ServerBinaryName { get; }

        /// <summary>Default full path to the Valheim dedicated-server executable.</summary>
        string DefaultServerPath { get; }

        /// <summary>Default folder Valheim itself writes world saves to.</summary>
        string DefaultSaveDataFolder { get; }

        /// <summary>The current (v2, JSON) user-preferences file path.</summary>
        string UserPrefsFilePath { get; }

        /// <summary>The legacy (v1, <c>.txt</c>) user-preferences file path, migrated from on first run.</summary>
        string LegacyUserPrefsFilePath { get; }

        /// <summary>The players-cache file path.</summary>
        string PlayerListFilePath { get; }

        /// <summary>The folder application and server logs are written to.</summary>
        string LogsFolderPath { get; }

        /// <summary>Resolves <paramref name="relativeName"/> under the application's per-user data folder.</summary>
        string GetAppDataPath(string relativeName);
    }

    /// <summary>
    /// Shares the relative app-data structure across OSes: the per-OS implementations supply only the
    /// roots (<see cref="AppDataRoot"/>, <see cref="DefaultServerPath"/>, <see cref="DefaultSaveDataFolder"/>)
    /// and <see cref="ServerBinaryName"/>; the file/folder layout beneath the app-data root is shared here.
    /// </summary>
    public abstract class ValheimPathResolver : IValheimPathResolver
    {
        // The layout beneath the app-data root is identical on every OS.
        protected const string UserPrefsFileName = "userprefs.json";
        protected const string LegacyUserPrefsFileName = "userprefs.txt";
        protected const string PlayerListFileName = "players-cache.json";
        protected const string LogsFolderName = "logs";

        public abstract string ServerBinaryName { get; }
        public abstract string DefaultServerPath { get; }
        public abstract string DefaultSaveDataFolder { get; }

        /// <summary>The per-user folder the application stores its own data (prefs, caches, logs) under.</summary>
        protected abstract string AppDataRoot { get; }

        public string UserPrefsFilePath => GetAppDataPath(UserPrefsFileName);
        public string LegacyUserPrefsFilePath => GetAppDataPath(LegacyUserPrefsFileName);
        public string PlayerListFilePath => GetAppDataPath(PlayerListFileName);
        public string LogsFolderPath => GetAppDataPath(LogsFolderName);

        public string GetAppDataPath(string relativeName) => Path.Join(AppDataRoot, relativeName);
    }
}
