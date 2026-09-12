using System.Diagnostics;

namespace ValheimServerGUI.Tools.Processes
{
    public interface IProcessProvider
    {
        void AddProcess(string key, Process process);

        Process? GetProcess(string key);

        void StartIO(Process process);

        /// <summary>
        /// Gracefully stops the tracked process with the given key, letting it shut down cleanly
        /// (so the Valheim server flushes a world save on the way down). OS-dispatched: Windows sends
        /// a close request via <c>taskkill /pid</c> (no <c>/f</c>); Linux sends SIGINT (never SIGKILL),
        /// exactly as Ctrl+C would. No-op if no live process is tracked under the key.
        /// </summary>
        void SafelyKillProcess(string key);

        /// <summary>
        /// Forcibly terminates the tracked process with the given key (Windows <c>taskkill /f</c>; Linux
        /// SIGKILL). Unlike <see cref="SafelyKillProcess"/> this does NOT let the server flush a world
        /// save — it is the last resort when a graceful stop has timed out (§16.2 stop-timeout). No-op if
        /// no live process is tracked under the key.
        /// </summary>
        void ForceKillProcess(string key);
    }
}
