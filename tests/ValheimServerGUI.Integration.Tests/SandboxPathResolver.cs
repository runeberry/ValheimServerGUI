using System.IO;
using ValheimServerGUI.Game;

namespace ValheimServerGUI.Integration.Tests
{
    /// <summary>
    /// Keeps the real OS resolver's server binary details but roots the application's own data (prefs, caches,
    /// logs, list baselines) and the default save folder under <paramref name="root"/>, so a live run never writes
    /// to the operator's real VSG data. Injected through DI rather than by redirecting environment variables,
    /// which the real server process would inherit.
    /// </summary>
    internal sealed class SandboxPathResolver(IValheimPathResolver real, string root) : ValheimPathResolver
    {
        public override string ServerBinaryName => real.ServerBinaryName;
        public override string DefaultServerPath => real.DefaultServerPath;
        public override string DefaultSaveDataFolder => Path.Join(root, "saves");
        protected override string AppDataRoot => Path.Join(root, "appdata");
    }
}
