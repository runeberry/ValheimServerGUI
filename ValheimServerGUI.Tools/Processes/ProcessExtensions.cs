﻿using System.Diagnostics;

namespace ValheimServerGUI.Tools.Processes
{
    public static class ProcessExtensions
    {
        // todo: Validate that taskkill exists on the system and that the user can access it
        private const string KillCommand = "taskkill";

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

            return provider.GetProcess(key);
        }

        /// <summary>
        /// Starts a secondary process to safely kill the provided process.
        /// Returns the provided process.
        /// </summary>
        public static Process SafelyKillProcess(this IProcessProvider provider, Process process)
        {
            if (process != null)
            {
                var killProcess = provider.AddBackgroundProcess($"{KillCommand}-{process.Id}", KillCommand, $"/pid {process.Id}");

                // todo: Send output to application logs
                provider.StartIO(killProcess);
            }

            return process;
        }

        /// <summary>
        /// Starts a secondary process to safely kill the process with the specified key.
        /// Returns the process with the specified key, which you can then wait for to exit.
        /// </summary>
        public static Process SafelyKillProcess(this IProcessProvider provider, string key)
        {
            return provider.SafelyKillProcess(provider.GetProcess(key));
        }

        public static bool IsProcessRunning(this IProcessProvider provider, string key)
        {
            var process = provider.GetProcess(key);

            if (process == null) return false;

            return !process.HasExited;
        }
    }
}
