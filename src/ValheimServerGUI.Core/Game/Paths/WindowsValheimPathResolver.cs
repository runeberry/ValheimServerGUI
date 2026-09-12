using System;

namespace ValheimServerGUI.Game
{
    /// <summary>
    /// Reproduces the Windows paths existing installs already use (the values the WinForms
    /// <c>Properties.Resources</c> carried), so upgrading in place keeps finding the same files.
    /// </summary>
    public class WindowsValheimPathResolver : ValheimPathResolver
    {
        public override string ServerBinaryName => "valheim_server.exe";

        public override string DefaultServerPath => Environment.ExpandEnvironmentVariables(
            @"%ProgramFiles(x86)%\Steam\steamapps\common\Valheim dedicated server\valheim_server.exe");

        public override string DefaultSaveDataFolder => Environment.ExpandEnvironmentVariables(
            @"%USERPROFILE%\AppData\LocalLow\IronGate\Valheim");

        protected override string AppDataRoot => Environment.ExpandEnvironmentVariables(
            @"%USERPROFILE%\AppData\LocalLow\Runeberry\ValheimServerGUI");
    }
}
