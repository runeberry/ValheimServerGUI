using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using ValheimServerGUI.Tools.Processes;

namespace ValheimServerGUI.Core.Tests.Fakes
{
    /// <summary>
    /// A headless process provider: it tracks the Process objects ValheimServer builds but never
    /// actually launches them (StartIO is a no-op). It records SafelyKillProcess calls and can
    /// simulate the OS raising Process.Exited, so the full server state machine is drivable without a
    /// real process.
    /// </summary>
    public class MockProcessProvider : IProcessProvider
    {
        private readonly ConcurrentDictionary<string, Process> Processes = new();

        /// <summary>Keys passed to SafelyKillProcess, in order.</summary>
        public List<string> SafelyKilledKeys { get; } = new();

        /// <summary>Keys passed to ForceKillProcess, in order (graceful-stop timeout fallback).</summary>
        public List<string> ForceKilledKeys { get; } = new();

        /// <summary>The most recently added (launched) process, for inspecting its StartInfo.Arguments.</summary>
        public Process? LastProcess { get; private set; }

        public void AddProcess(string key, Process process)
        {
            Processes[key] = process;
            LastProcess = process;
        }

        public Process? GetProcess(string key) => Processes.TryGetValue(key, out var p) ? p : null;

        public void StartIO(Process process)
        {
            // no-op: never actually launch anything in tests
        }

        public void SafelyKillProcess(string key)
        {
            SafelyKilledKeys.Add(key);
        }

        public void ForceKillProcess(string key)
        {
            ForceKilledKeys.Add(key);
        }

        /// <summary>Test hook: raise Process.Exited on the most recently launched process.</summary>
        public void SimulateExit()
        {
            if (LastProcess == null) return;
            typeof(Process)
                .GetMethod("OnExited", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(LastProcess, null);
        }
    }
}
