using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace ValheimServerGUI.Tools
{
    public class AppLogger : ILogger
    {
        public readonly string Name;
        public event EventHandler<LogEvent> LogReceived;
        
        private const int LogBufferSize = 10000;
        private readonly Queue<LogEvent> Logs;

        public AppLogger(string name)
        {
            this.Name = name;
            this.Logs = new Queue<LogEvent>(LogBufferSize);
        }

        public IEnumerable<LogEvent> GetLogs()
        {
            return this.Logs;
        }

        #region ILogger implementation

        public IDisposable BeginScope<TState>(TState state)
        {
            return default;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var rawMessage = formatter(state, exception);

            var logEvent = new LogEvent
            {
                LogLevel = logLevel,
                Timestamp = DateTime.Now,
                EventId = eventId.ToString(),
                Message = formatter(state, exception),
            };

            AddLogToBuffer(logEvent);

            LogReceived?.Invoke(this, logEvent);
        }

        #endregion

        #region Non-public methods

        private void AddLogToBuffer(LogEvent logEvent)
        {
            if (this.Logs.Count >= LogBufferSize)
            {
                // Need to remove the oldest log before adding to the buffer
                this.Logs.Dequeue();
            }

            this.Logs.Enqueue(logEvent);
        }

        #endregion
    }
}
