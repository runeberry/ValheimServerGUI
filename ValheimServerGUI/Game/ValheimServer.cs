using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValheimServerGUI.Game
{
    public class ValheimServer : IDisposable
    {
        public string ServerPath { get; set; }
        public string ServerName { get; set; }
        public string ServerPassword { get; set; }
        public string WorldName { get; set; }
        public bool Public { get; set; }
        
        public bool IsRunning => this.Process != null;

        private Process Process;
        private readonly List<Action<string>> Listeners = new List<Action<string>>();

        public ValheimServer()
        {
            
        }

        public void Validate()
        {
            // Ensure all required fields eixst
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
            if (this.IsRunning) return;

            this.Validate();

            this.Process = this.BuildProcess();
            
            this.Process.Start();
            this.Process.BeginOutputReadLine();
            this.Process.BeginErrorReadLine();
        }

        public void Stop()
        {
            if (!this.IsRunning) return;

            this.Process.Close();

            this.Process.Dispose();
            this.Process = null;
        }

        public void AddListener(Action<string> consoleListener)
        {
            this.Listeners.Add(consoleListener);
        }

        #region Non-public methods

        private Process BuildProcess()
        {
            var publicFlag = this.Public ? 1 : 0;

            var process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo =
                {
                    FileName = this.ServerPath,
                    Arguments = @$"-nographics -batchmode -name ""{this.ServerName}"" -port 2456 -world ""{this.WorldName}"" -password ""{this.ServerPassword}"" -public {publicFlag}",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                },
            };

            process.StartInfo.EnvironmentVariables.Add("SteamAppId", "892970"); // From: start_headless_server.bat
            process.OutputDataReceived += new DataReceivedEventHandler(this.Process_OnDataReceived);
            process.ErrorDataReceived += new DataReceivedEventHandler(this.Process_OnDataReceived);

            return process;
        }

        private void Process_OnDataReceived(object obj, DataReceivedEventArgs e)
        {
            foreach (var listener in this.Listeners)
            {
                listener(e.Data);
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
