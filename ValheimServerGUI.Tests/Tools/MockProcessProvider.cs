using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using ValheimServerGUI.Tools.Processes;

namespace ValheimServerGUI.Tests.Tools
{
    public class MockProcessProvider : IProcessProvider
    {
        private readonly ConcurrentDictionary<string, Process> Processes = new();

        public void AddProcess(string key, Process process)
        {
            if (!Processes.TryAdd(key, process))
            {
                throw new InvalidOperationException($"Failed to add process with key: {key}");
            }
        }

        public Process GetProcess(string key)
        {
            if (!Processes.TryGetValue(key, out var process)) return null;
            return process;
        }


        public void StartIO(Process process)
        {
            // no-op for testing
        }
    }
}
