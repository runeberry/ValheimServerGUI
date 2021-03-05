using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ValheimServerGUI.Properties;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Processes;

namespace ValheimServerGUI.Game
{
    public class ValheimServer : IDisposable
    {
        public string ServerPath { get; set; }
        public string ServerName { get; set; }
        public string ServerPassword { get; set; }
        public string WorldName { get; set; }
        public bool Public { get; set; }

        public event EventHandler<ServerStatus> StatusChanged;

        public bool IsStarting { get; private set; }
        public bool IsStopping { get; private set; }
        public bool IsRestarting { get; private set; }
        public bool IsRunning => this.ProcessProvider.IsProcessRunning(ProcessKeys.ValheimServer);

        public readonly AppLogger Logger;
        public readonly FilteredServerLogger FilteredLogger;
        private readonly Dictionary<string, Action> LogBasedActions = new Dictionary<string, Action>();

        private readonly IProcessProvider ProcessProvider;
        private Process ServerProcess => this.ProcessProvider.GetProcess(ProcessKeys.ValheimServer);

        public ValheimServer(IProcessProvider processProvider)
        {
            this.ProcessProvider = processProvider;

            Logger = new AppLogger();
            FilteredLogger = new FilteredServerLogger();

            Logger.LogReceived += new EventHandler<LogEvent>(this.Logger_OnServerLogReceived);

            InitializeLogBasedActions();
            InitializeStatusBasedActions();
        }

        #region Initialization

        private void InitializeLogBasedActions()
        {
            LogBasedActions.Add("Game server connected", () => this.PublishStatus(ServerStatus.Started));
            LogBasedActions.Add("Steam manager on destroy", () => this.PublishStatus(ServerStatus.Stopped));
        }

        private void InitializeStatusBasedActions()
        {
            this.StatusChanged += BuildStatusHandler(ServerStatus.Started, () => this.IsStarting = false);
            this.StatusChanged += BuildStatusHandler(ServerStatus.Stopped, () => this.IsStopping = false);
            this.StatusChanged += BuildStatusHandler(ServerStatus.ProcessExited, () => 
            {
                this.IsStopping = false;

                if (this.IsRestarting)
                {
                    this.IsRestarting = false;
                    this.Start();
                }
            });
        }

        private static EventHandler<ServerStatus> BuildStatusHandler(ServerStatus status, Action action)
        {
            return new EventHandler<ServerStatus>((obj, s) =>
            {
                if (s == status) action();
            });
        }

        #endregion

        #region Public methods

        public void Validate()
        {
            // Ensure all required fields exist
            if (string.IsNullOrWhiteSpace(this.ServerName)) throw new ArgumentException($"{nameof(ServerName)} must be defined.");
            if (string.IsNullOrWhiteSpace(this.ServerPath)) throw new ArgumentException($"{nameof(ServerPath)} must be defined.");
            if (string.IsNullOrWhiteSpace(this.ServerPassword)) throw new ArgumentException($"{nameof(ServerPassword)} must be defined.");
            if (string.IsNullOrWhiteSpace(this.WorldName)) throw new ArgumentException($"{nameof(WorldName)} must be defined.");

            // ServerPath validation
            if (!this.ServerPath.EndsWith(".exe")) throw new ArgumentException($"{nameof(ServerPath)} must point to a valid .exe file.");
            if (!File.Exists(this.ServerPath)) throw new FileNotFoundException($"No file found at {nameof(ServerPath)}: {this.ServerPath}");

            // WorldName validation
            // todo: Validate world exists? Or do we trust it from the UI control?

            // ServerName validation
            if (this.ServerName == this.WorldName) throw new ArgumentException($"{nameof(ServerName)} cannot be the same as your {nameof(WorldName)} ({this.WorldName}).");

            // ServerPassword validation
            if (this.ServerPassword.Length < 5) throw new ArgumentException($"{nameof(ServerPassword)} must be at least 5 characters.");
            if (this.ServerPassword.Contains(this.ServerName)) throw new ArgumentException($"{nameof(ServerPassword)} cannot contain your {nameof(ServerName)} ({this.ServerName}).");
            if (this.ServerPassword.Contains(this.WorldName)) throw new ArgumentException($"{nameof(ServerPassword)} cannot contain your {nameof(WorldName)} ({this.WorldName}).");
        }

        public void Start()
        {
            if (this.IsRunning || this.IsStarting) return;

            this.Validate();

            var publicFlag = this.Public ? 1 : 0;
            var processArgs = @$"-nographics -batchmode -name ""{this.ServerName}"" -port 2456 -world ""{this.WorldName}"" -password ""{this.ServerPassword}"" -public {publicFlag}";
            var process = this.ProcessProvider.AddBackgroundProcess(ProcessKeys.ValheimServer, this.ServerPath, processArgs);

            process.StartInfo.EnvironmentVariables.Add("SteamAppId", Resources.ValheimSteamAppId);
            process.OutputDataReceived += new DataReceivedEventHandler(this.Process_OnDataReceived);
            process.ErrorDataReceived += new DataReceivedEventHandler(this.Process_OnErrorReceived);
            process.Exited += new EventHandler(this.Process_OnExited);

            process.StartIO();

            this.IsStarting = true;
            PublishStatus(ServerStatus.StartRequested);
        }

        public void Stop()
        {
            if (!this.IsRunning || this.IsStopping) return;

            this.ProcessProvider.SafelyKillProcess(ProcessKeys.ValheimServer);

            this.IsStopping = true;
            PublishStatus(ServerStatus.StopRequested);
        }

        public void Restart()
        {
            if (!this.IsRunning || this.IsStarting || this.IsStopping) return;

            this.ProcessProvider.SafelyKillProcess(ProcessKeys.ValheimServer);

            this.IsStopping = true;
            this.IsRestarting = true;
            PublishStatus(ServerStatus.StopRequested);
        }

        public void ForceStop()
        {
            this.Stop();

            if (this.IsRunning || this.IsStopping)
            {
                this.ServerProcess.WaitForExit();
            }
        }

        #endregion

        #region Event handlers

        private void Process_OnDataReceived(object obj, DataReceivedEventArgs e)
        {
            Logger.LogInformation(e.Data);
            FilteredLogger.LogInformation(e.Data);
        }

        private void Process_OnErrorReceived(object obj, DataReceivedEventArgs e)
        {
            Logger.LogError(e.Data);
            FilteredLogger.LogError(e.Data);
        }

        private void Process_OnExited(object obj, EventArgs e)
        {
            PublishStatus(ServerStatus.ProcessExited);
        }

        private void Logger_OnServerLogReceived(object obj, LogEvent logEvent)
        {
            foreach (var action in this.LogBasedActions
                .Where(kvp => Regex.IsMatch(logEvent.Message, kvp.Key))
                .Select(kvp => kvp.Value))
            {
                action();
            }
        }

        #endregion

        #region Common methods

        private void PublishStatus(ServerStatus status)
        {
            this.StatusChanged?.Invoke(this, status);
        }

        #endregion

        #region IDisposable implementation

        public void Dispose()
        {
            this.Stop();

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
