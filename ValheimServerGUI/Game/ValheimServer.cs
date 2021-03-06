using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using ValheimServerGUI.Properties;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Processes;

namespace ValheimServerGUI.Game
{
    public class ValheimServer : IDisposable
    {
        /// <summary>
        /// Options for the currently running server.
        /// </summary>        
        public IValheimServerOptions Options { get; private set; } = new ValheimServerOptions();

        public ServerStatus Status 
        {
            get => this._status;
            private set
            {
                if (this._status == value) return;
                this._status = value;
                this.StatusChanged?.Invoke(this, value);
            }
        }
        private ServerStatus _status = ServerStatus.Stopped;
        public event EventHandler<ServerStatus> StatusChanged;

        public bool CanStart => this.IsAnyStatus(ServerStatus.Stopped);
        public bool CanStop => this.IsAnyStatus(ServerStatus.Starting, ServerStatus.Running);
        public bool CanRestart => this.IsAnyStatus(ServerStatus.Running);

        private bool IsRestarting;

        // todo: fully encapsulate the loggers in this class
        public readonly AppLogger Logger;
        public readonly FilteredServerLogger FilteredLogger;
        private readonly Dictionary<string, Action> LogBasedActions = new Dictionary<string, Action>();

        private readonly IProcessProvider ProcessProvider;
        private Process ServerProcess => this.ProcessProvider.GetProcess(ProcessKeys.ValheimServer);

        public ValheimServer(IProcessProvider processProvider)
        {
            this.ProcessProvider = processProvider;

            // todo: dependency-inject loggers
            Logger = new AppLogger();
            FilteredLogger = new FilteredServerLogger();

            Logger.LogReceived += new EventHandler<LogEvent>(this.Logger_OnServerLogReceived);

            InitializeLogBasedActions();
            InitializeStatusBasedActions();
        }

        #region Initialization

        private void InitializeLogBasedActions()
        {
            LogBasedActions.Add("Game server connected", () => this.Status = ServerStatus.Running);
            // LogBasedActions.Add("Steam manager on destroy", () => this.Status = ServerStatus.Stopped); // Now based on process exit
        }

        private void InitializeStatusBasedActions()
        {
            this.StatusChanged += BuildStatusHandler(ServerStatus.Stopped, () => 
            {
                if (this.IsRestarting)
                {
                    this.IsRestarting = false;
                    this.Start(this.Options);
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

        /// <summary>
        /// Starts the Valheim server as a background process with the provided options.
        /// </summary>
        public void Start(IValheimServerOptions options)
        {
            if (!this.CanStart) return;

            var publicFlag = options.Public ? 1 : 0;
            var processArgs = @$"-nographics -batchmode -name ""{options.Name}"" -port {options.Port} -world ""{options.WorldName}"" -password ""{options.Password}"" -public {publicFlag}";
            var process = this.ProcessProvider.AddBackgroundProcess(ProcessKeys.ValheimServer, options.ExePath, processArgs);

            process.StartInfo.EnvironmentVariables.Add("SteamAppId", Resources.ValheimSteamAppId);
            process.OutputDataReceived += new DataReceivedEventHandler(this.Process_OnDataReceived);
            process.ErrorDataReceived += new DataReceivedEventHandler(this.Process_OnErrorReceived);
            process.Exited += new EventHandler((obj, e) => this.Status = ServerStatus.Stopped);

            process.StartIO();

            this.Options = options;
            this.IsRestarting = false;
            this.Status = ServerStatus.Starting;
        }

        /// <summary>
        /// Gracefully stops the Valheim server process.
        /// </summary>
        public void Stop()
        {
            if (!this.CanStop) return;

            this.ProcessProvider.SafelyKillProcess(ProcessKeys.ValheimServer);

            this.IsRestarting = false;
            this.Status = ServerStatus.Stopping;
        }

        /// <summary>
        /// Gracefully stops the Valheim server process, then starts it up again.
        /// If no options are provided, then the existing server options will be used.
        /// </summary>
        public void Restart(IValheimServerOptions options = null)
        {
            if (!this.CanRestart) return;

            this.ProcessProvider.SafelyKillProcess(ProcessKeys.ValheimServer);

            this.Options = options ?? this.Options;
            this.IsRestarting = true;
            this.Status = ServerStatus.Stopping;
        }

        public void ForceStop()
        {
            this.Stop();

            if (!this.IsAnyStatus(ServerStatus.Stopped))
            {
                // todo: replace this with something that isn't as likely to hang
                // Maybe remove this method altogether and build something better in the UI?
                this.ServerProcess.WaitForExit();
            }
        }

        public bool IsAnyStatus(params ServerStatus[] statuses)
        {
            return statuses.Any(s => s == this.Status);
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

        #region IDisposable implementation

        public void Dispose()
        {
            this.Stop();

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
