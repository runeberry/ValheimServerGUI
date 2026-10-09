using System;

namespace ValheimServerGUI.Game
{
    /// <summary>
    /// Reproduces the Windows paths existing installs already use (the values the WinForms
    /// <c>Properties.Resources</c> carried), so upgrading in place keeps finding the same files.
    /// </summary>
    public class WindowsValheimPathResolver : ValheimPathResolver
    {
        private readonly Func<string, string> _expand;

        public WindowsValheimPathResolver() : this(Environment.ExpandEnvironmentVariables)
        {
        }

        /// <summary>Test seam: inject the OS boundary (environment-variable expansion).</summary>
        internal WindowsValheimPathResolver(Func<string, string> expandEnvironmentVariables)
        {
            _expand = expandEnvironmentVariables;
        }

        public override string ServerBinaryName => "valheim_server.exe";

        public override string DefaultServerPath =>
            _expand(@"%ProgramFiles(x86)%\Steam\steamapps\common\Valheim dedicated server\valheim_server.exe");

        public override string DefaultSaveDataFolder => _expand(@"%USERPROFILE%\AppData\LocalLow\IronGate\Valheim");

        protected override string AppDataRoot => _expand(@"%USERPROFILE%\AppData\LocalLow\Runeberry\ValheimServerGUI");
    }
}
