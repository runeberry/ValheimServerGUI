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
            get => _logView;
            set
            {
                value ??= DefaultLogView;
                if (value == _logView) return;
                _logView = value;
                LogViewChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler LogViewChanged;

        public string TimestampFormat { get; set; }

        public LogViewer()
        {
            InitializeComponent();

            LogViewChanged += OnLogViewChanged;
        }

        public void AddLog(string message)
        {
            AddLogToView(message, DefaultLogView);
        }

        public void AddLogToView(string message, string viewName)
        {
            message = StoreLog(message, viewName);

            if (viewName == LogView)
            {
                AppendLine(message);
            }
        }

        public void ClearLogs()
        {
            LogsByView.Clear();

            ClearLines();
        }

        public void ClearLogView(string viewName)
        {
            if (viewName == null || !LogsByView.TryGetValue(viewName, out var logs)) return;

            logs.Clear();

            if (viewName == LogView)
            {
                ClearLines();
            }
        }

        public string GetCurrentViewText()
        {
            return TextBox.Text;
        }

        #region Non-public methods

        private string StoreLog(string message, string viewName)
        {
            if (!LogsByView.TryGetValue(viewName, out var list))
            {
                list = new List<string>();
                if (!LogsByView.TryAdd(viewName, list))
                {
                    list = LogsByView[viewName];
                }
            }

            if (TimestampFormat != null)
            {
                message = $"[{DateTime.Now.ToString(TimestampFormat)}] {message}";
            }

            list.Add(message);

            return message;
        }

        private void AppendLine(string line)
        {
            if (line == null) return;

            if (TextBox.Text == null || TextBox.Text.Length == 0)
            {
                TextBox.Text = line;
            }
            else
            {
                TextBox.AppendText(Environment.NewLine + line);
            }
        }

        private void ClearLines()
        {
            TextBox.Text = string.Empty;
        }

        private void OnLogViewChanged(object sender, EventArgs args)
        {
            ClearLines();

            if (LogsByView.TryGetValue(LogView, out var logs))
            {
                var logString = string.Join(Environment.NewLine, logs);

                AppendLine(logString);
            }
        }

        #endregion
    }
}
