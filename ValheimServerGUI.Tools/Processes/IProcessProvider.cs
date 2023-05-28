using System.Diagnostics;

namespace ValheimServerGUI.Tools.Processes
{
    public interface IProcessProvider
    {
        void AddProcess(string key, Process process);

        Process GetProcess(string key);

        void StartIO(Process process);
    }
}
