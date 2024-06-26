﻿using Serilog;
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
    }

    /// <summary>
    /// Preconfigured Logger for use throughout the application.
    /// </summary>
    public abstract class BaseLogger : IBaseLogger
    {
        private readonly LogBufferSink LogBufferSink = new(1000);
        private readonly List<LogRule> Rules = new();
        private ILogger Logger;

        #region IBaseLogger implementation

        public event Action<string> LogReceived;

        public IEnumerable<string> LogBuffer => LogBufferSink.Logs;

        #endregion

        #region Protected methods

        /// <summary>
        /// Adds a rule to filter or transform messages in this logger. Order matters!
        /// </summary>
        /// <param name="rule"></param>
        protected void AddRule(LogRule rule)
        {
            Rules.Add(rule);
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

        protected void AddFileLogging(LoggerConfiguration config, string fileName)
        {
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                config.WriteToRollingFile(Resources.LogsFolderPath, fileName);
            }
        }

        #endregion

        #region Private methods

        private ILogger CreateLogger()
        {
            var config = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Verbose()
#else
                .MinimumLevel.Debug()
#endif
                .WriteTo.Sink(LogBufferSink);

            ConfigureLogger(config);

            return config.CreateLogger();
        }

        #endregion

        #region ILogger implementation

        public void Write(LogEvent logEvent)
        {
            // Wait until first log is written to create logger, so all dependencies are resolved
            Logger ??= CreateLogger();

            var message = logEvent.RenderMessage();

            foreach (var rule in Rules)
            {
                var include = rule.Include(logEvent, message);

                // Exclude message at the first filter which returns false
                if (!include) return;

                // Update the rendered message with each pass
                message = rule.Transform(logEvent, message);
            }

            Logger.Write(logEvent.Level, message);

            LogReceived?.Invoke(message);
        }

        #endregion
    }
}
