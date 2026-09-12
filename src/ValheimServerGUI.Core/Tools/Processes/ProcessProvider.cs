using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace ValheimServerGUI.Tools.Processes
{
    public class ProcessProvider : IProcessProvider
    {
        private readonly ConcurrentDictionary<string, Process> Processes = new();

        public void AddProcess(string key, Process process)
        {
            if (!Processes.TryAdd(key, process))
            {
                throw new InvalidOperationException($"Failed to add process with key: {key}");
            }

            // Remove this process from the provider when the process exits.
            process.Exited += (_, _) => Processes.TryRemove(key, out var _);
        }

        public Process? GetProcess(string key)
        {
            if (!Processes.TryGetValue(key, out var process)) return null;

            try
            {
                // Don't return a process if it's already exited
                if (process.HasExited) return null;
            }
            catch
            {
                // HasExited will throw if the process hasn't started yet, so ignore that
            }

            return process;
        }

        public void StartIO(Process process)
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        public void SafelyKillProcess(string key)
        {
            var process = GetProcess(key);
            if (process == null) return;

            // Launch a secondary process to deliver the graceful-stop signal, mirroring how the
            // Windows path has always worked. The goal is a clean shutdown that lets the server save.
            Process killProcess;
            if (OperatingSystem.IsWindows())
            {
                // taskkill WITHOUT /f: a close request, not a forced kill.
                killProcess = this.AddBackgroundProcess($"taskkill-{process.Id}", "taskkill", $"/pid {process.Id}");
            }
            else
            {
                // SIGINT (signal 2), never SIGKILL -- the server traps it and flushes a world save,
                // exactly as pressing Ctrl+C in its console would.
                killProcess = this.AddBackgroundProcess($"kill-{process.Id}", "kill", $"-s INT {process.Id}");
            }

            // todo: Send output to application logs
            StartIO(killProcess);
        }
    }
}
