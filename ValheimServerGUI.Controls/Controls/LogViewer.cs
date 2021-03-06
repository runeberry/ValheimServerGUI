﻿using System;
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

        public string TimestampFormat { get; set; }

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
            message = this.StoreLog(message, viewName);

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

        public void ClearLogView(string viewName)
        {
            if (viewName == null || !this.LogsByView.TryGetValue(viewName, out var logs)) return;

            logs.Clear();

            if (viewName == this.LogView)
            {
                this.ClearLines();
            }
        }

        public string GetCurrentViewText()
        {
            return this.TextBox.Text;
        }

        #region Non-public methods

        private string StoreLog(string message, string viewName)
        {
            if (!this.LogsByView.TryGetValue(viewName, out var list))
            {
                list = new List<string>();
                if (!this.LogsByView.TryAdd(viewName, list))
                {
                    list = this.LogsByView[viewName];
                }
            }

            if (this.TimestampFormat != null)
            {
                message = $"[{DateTime.Now.ToString(this.TimestampFormat)}] {message}";
            }

            list.Add(message);

            return message;
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
                var logString = string.Join(Environment.NewLine, logs);

                this.AppendLine(logString);
            }
        }

        #endregion
    }
}
