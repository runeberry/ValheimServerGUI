using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ValheimServerGUI.Tools.Processes
{
    public class ProcessProvider : IProcessProvider
    {
        private readonly ConcurrentDictionary<string, Process> Processes = new ConcurrentDictionary<string, Process>();

        public void AddProcess(string key, Process process)
        {
            if (!Processes.TryAdd(key, process))
            {
                throw new InvalidOperationException($"Failed to add process with key: {key}");
            }

            // Remove this process from the provider when the process exits.
            process.Exited += (_, _) => Processes.TryRemove(key, out var _);
        }

        public Process GetProcess(string key)
        {
            if (!Processes.TryGetValue(key, out var process)) return null;
            if (process.HasExited) return null;
            return process;
        }

        public List<Process> FindExistingProcessesByName(string name)
        {
            return Process.GetProcesses()
                .Where(p => p.ProcessName == name)
                .ToList();
        }
    }
}
