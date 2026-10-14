using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Serilog;

namespace ValheimServerGUI.Tools.Processes
{
    public class ProcessProvider : IProcessProvider
    {
        private readonly ConcurrentDictionary<string, Process> Processes = new();
        private readonly ILogger Logger;
        private readonly Func<int, (string Command, string Arguments)> GracefulStopCommand;

        public ProcessProvider(ILogger logger) : this(logger, DefaultGracefulStopCommand)
        {
        }

        /// <summary>Test seam: inject the command that asks the process with the given id to stop.</summary>
        internal ProcessProvider(ILogger logger, Func<int, (string Command, string Arguments)> gracefulStopCommand)
        {
            Logger = logger;
            GracefulStopCommand = gracefulStopCommand;
        }

        // A secondary process delivers the graceful-stop signal, so the server shuts down cleanly and saves.
        private static (string Command, string Arguments) DefaultGracefulStopCommand(int processId)
            => OperatingSystem.IsWindows()
                // taskkill WITHOUT /f: a close request, not a forced kill.
                ? ("taskkill", $"/pid {processId}")
                // SIGINT (signal 2), never SIGKILL -- the server traps it and flushes a world save,
                // exactly as pressing Ctrl+C in its console would.
                : ("kill", $"-s INT {processId}");

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

            var processId = process.Id;
            var (command, arguments) = GracefulStopCommand(processId);
            var stopProcess = this.AddBackgroundProcess($"stop-{processId}", command, arguments);

            // A refused stop request (e.g. taskkill's "can only be terminated forcefully") leaves the server running
            // with no other sign of why, so record what the helper said when it fails.
            var output = new ConcurrentQueue<string>();
            stopProcess.OutputDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) output.Enqueue(e.Data.Trim()); };
            stopProcess.ErrorDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) output.Enqueue(e.Data.Trim()); };
            stopProcess.Exited += (_, _) =>
            {
                // Exited can fire before the redirected streams drain; wait for them so the output is complete.
                stopProcess.WaitForExit();
                if (stopProcess.ExitCode == 0) return;

                Logger.Warning("Stop request for server process {processId} failed: {command} {arguments} exited with code {exitCode}: {output}",
                    processId, command, arguments, stopProcess.ExitCode, string.Join(" ", output));
            };

            try
            {
                StartIO(stopProcess);
            }
            catch (Exception e)
            {
                Processes.TryRemove($"stop-{processId}", out _);
                Logger.Error(e, "Could not send a stop request to server process {processId}: {command} {arguments} could not be started",
                    processId, command, arguments);
            }
        }
    }
}
