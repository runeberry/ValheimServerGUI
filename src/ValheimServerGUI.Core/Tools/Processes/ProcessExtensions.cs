using System.Diagnostics;

namespace ValheimServerGUI.Tools.Processes
{
    public static class ProcessExtensions
    {
        public static Process AddBackgroundProcess(this IProcessProvider provider, string key, string command, string args)
        {
            var process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo =
                {
                    FileName = command,
                    Arguments = args,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                },
            };

            provider.AddProcess(key, process);

            // The provider tracks the same instance we just added, so this is non-null.
            return provider.GetProcess(key)!;
        }

        public static bool IsProcessRunning(this IProcessProvider provider, string key)
        {
            var process = provider.GetProcess(key);

            if (process == null) return false;

            return !process.HasExited;
        }
    }
}
