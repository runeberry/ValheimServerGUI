using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ValheimServerGUI.Controls
{
    public partial class LogViewer : UserControl
    {
        public const string DefaultLogView = "DefaultLogView";

        private readonly ConcurrentDictionary<string, List<string>> LogsByView = new();

        private string _logView = DefaultLogView;
        public string LogView
        {
            get => this._logView;
            set
            {
                value ??= DefaultLogView;
                if (value == this._logView) return;
                this._logView = value;
                this.LogViewChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler LogViewChanged;

        public LogViewer()
        {
            InitializeComponent();

            this.LogViewChanged += this.OnLogViewChanged;
        }

        public void AddLog(string message)
        {
            this.AddLogToView(message, DefaultLogView);
        }

        public void AddLogToView(string message, string viewName)
        {
            this.StoreLog(message, viewName);

            if (viewName == this.LogView)
            {
                this.AppendLine(message);
            }
        }

        public void ClearLogs()
        {
            this.LogsByView.Clear();
            
            this.ClearLines();
        }

        public string GetCurrentViewText()
        {
            return this.TextBox.Text;
        }

        #region Non-public methods

        private void StoreLog(string message, string viewName)
        {
            if (!this.LogsByView.TryGetValue(viewName, out var list))
            {
                list = new List<string>();
                if (!this.LogsByView.TryAdd(viewName, list))
                {
                    list = this.LogsByView[viewName];
                }
            }

            list.Add(message);
        }

        private void AppendLine(string line)
        {
            if (line == null) return;

            if (this.TextBox.Text == null || this.TextBox.Text.Length == 0)
            {
                this.TextBox.Text = line;
            }
            else
            {
                this.TextBox.AppendText(Environment.NewLine + line);
            }
        }

        private void ClearLines()
        {
            this.TextBox.Text = string.Empty;
        }

        private void OnLogViewChanged(object sender, EventArgs args)
        {
            this.ClearLines();

            if (this.LogsByView.TryGetValue(this.LogView, out var logs))
            {
                foreach (var log in logs)
                {
                    this.AppendLine(log);
                }
            }
        }

        #endregion
    }
}
