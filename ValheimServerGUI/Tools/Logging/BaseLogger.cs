using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using ValheimServerGUI.Properties;
using ValheimServerGUI.Tools.Logging.Components;

namespace ValheimServerGUI.Tools.Logging
{
    public interface IBaseLogger : ILogger
    {
        event Action<string> LogReceived;

        IEnumerable<string> LogBuffer { get; }

        void SetFileLoggingEnabled(bool enabled);
    }

    /// <summary>
    /// Preconfigured Logger for use throughout the application.
    /// </summary>
    public abstract class BaseLogger
    {
        public delegate string LogEventModifier(LogEvent logEvent, string renderedMessage);

        private readonly LogBufferSink LogBufferSink = new(1000);
        private readonly List<LogEventModifier> Modifiers = new();
        private bool FileLoggingEnabled;
        private ILogger Logger;

        public BaseLogger()
        {
            RebuildLogger();
        }

        #region IBaseLogger implementation

        public event Action<string> LogReceived;

        public IEnumerable<string> LogBuffer => LogBufferSink.Logs;

        public void SetFileLoggingEnabled(bool enabled)
        {
            if (enabled == FileLoggingEnabled) return;

            FileLoggingEnabled = enabled;
            RebuildLogger();
        }

        #endregion

        #region Protected methods

        protected void AddModifier(LogEventModifier modifier)
        {
            Modifiers.Add(modifier);
        }

        /// <summary>
        /// Implement custom logging configuration specific to your class here.
        /// </summary>
        protected virtual void ConfigureLogger(LoggerConfiguration config)
        {
            // no-op by default
        }

        /// <summary>
        /// Set an output name for your log file, if file logging is enabled.
        /// </summary>
        protected virtual string LogFileName => null;

        protected void RebuildLogger()
        {
            Logger = CreateLogger();
        }

        #endregion

        #region Private methods

        private ILogger CreateLogger()
        {
            var config = new LoggerConfiguration()
                .WriteTo.Sink(LogBufferSink);

            if (FileLoggingEnabled)
            {
                var fileName = LogFileName;
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    config.WriteToRollingFile(Resources.LogsFolderPath, fileName);
                }
            }

            ConfigureLogger(config);

            return config.CreateLogger();
        }

        #endregion

        #region ILogger implementation

        public void Write(LogEvent logEvent)
        {
            var message = logEvent.RenderMessage();

            foreach (var modifier in Modifiers)
            {
                message = modifier(logEvent, message);
            }

            message = $"{logEvent.Timestamp.ToLogPrefixFormat()} {message}";

            Logger.Write(logEvent.Level, message);

            LogReceived?.Invoke(message);
        }

        #endregion
    }
}
