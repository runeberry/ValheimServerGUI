using System.Diagnostics;

namespace ValheimServerGUI.Tools.Processes
{
    public interface IProcessProvider
    {
        public Process GetProcess(string key);

        public void AddProcess(string key, Process process);
    }
}
