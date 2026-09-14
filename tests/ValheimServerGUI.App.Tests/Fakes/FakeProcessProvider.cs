using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using ValheimServerGUI.Tools.Processes;

namespace ValheimServerGUI.App.Tests.Fakes;

/// <summary>
/// Headless process provider (mirrors the Core test mock): tracks the Process objects ValheimServer builds
/// but never launches them, and can simulate the OS raising Process.Exited so a server can be driven through
/// its state machine (Starting → Running via the connected log line → Stopped) with no real process.
/// </summary>
public sealed class FakeProcessProvider : IProcessProvider
{
    private readonly ConcurrentDictionary<string, Process> _processes = new();

    public Process? LastProcess { get; private set; }

    public void AddProcess(string key, Process process)
    {
        _processes[key] = process;
        LastProcess = process;
    }

    public Process? GetProcess(string key) => _processes.TryGetValue(key, out var p) ? p : null;

    public void StartIO(Process process) { /* no-op: never launch in tests */ }

    public void SafelyKillProcess(string key) { }

    public void ForceKillProcess(string key) { }

    /// <summary>Raise Process.Exited on the most recently launched process (→ server reports Stopped).</summary>
    public void SimulateExit()
    {
        if (LastProcess is null) return;
        typeof(Process)
            .GetMethod("OnExited", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(LastProcess, null);
    }
}
