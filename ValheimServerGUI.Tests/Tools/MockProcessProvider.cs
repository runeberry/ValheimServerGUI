using System;
using System.Collections.Generic;
using System.Diagnostics;
using ValheimServerGUI.Tools.Processes;

namespace ValheimServerGUI.Tests.Tools
{
    public class MockProcessProvider : IProcessProvider
    {
        public void AddProcess(string key, Process process)
        {
            throw new NotImplementedException("Cannot add processes in unit tests!");
        }

        public Process GetProcess(string key)
        {
            throw new NotImplementedException("Cannot get processes in unit tests!");
        }

        public List<Process> FindExistingProcessesByName(string name)
        {
            return new();
        }
    }
}
