using System;
using System.IO;

namespace ValheimServerGUI.Game
{
    /// <summary>
    /// Resolves Valheim paths using Linux XDG / <c>$HOME</c> conventions. The dedicated-server binary
    /// is <c>valheim_server.x86_64</c>; the server writes saves under <c>~/.config/unity3d/IronGate/Valheim</c>;
    /// the application stores its own data under <c>$XDG_DATA_HOME/ValheimServerGUI</c>
    /// (<c>~/.local/share/ValheimServerGUI</c> by default).
    /// </summary>
    public class LinuxValheimPathResolver : ValheimPathResolver
    {
        private readonly string _home;
        private readonly string _xdgDataHome;

        public LinuxValheimPathResolver()
            : this(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                   Environment.GetEnvironmentVariable("XDG_DATA_HOME"))
        {
        }

        /// <summary>Test seam: inject the OS boundary (home directory and <c>XDG_DATA_HOME</c>).</summary>
        internal LinuxValheimPathResolver(string home, string? xdgDataHome)
        {
            _home = home;
            _xdgDataHome = string.IsNullOrEmpty(xdgDataHome) ? Path.Join(home, ".local", "share") : xdgDataHome;
        }

        public override string ServerBinaryName => "valheim_server.x86_64";

        public override string DefaultServerPath =>
            Path.Join(_home, ".steam", "steam", "steamapps", "common", "Valheim dedicated server", "valheim_server.x86_64");

        public override string DefaultSaveDataFolder =>
            Path.Join(_home, ".config", "unity3d", "IronGate", "Valheim");

        protected override string AppDataRoot =>
            Path.Join(_xdgDataHome, "ValheimServerGUI");
    }
}
