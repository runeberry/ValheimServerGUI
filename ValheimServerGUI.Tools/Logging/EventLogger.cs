using Microsoft.Extensions.Logging;
using System;

namespace ValheimServerGUI.Tools.Logging
{
    public class EventLogger : IEventLogger
    {
        protected string CategoryName { get; set; }

        protected virtual bool FilterLog(EventLogContext context)
        {
            return true;
        }

        protected virtual string FormatLog(EventLogContext context)
        {
            return context.Message;
        }

        #region IEventLogger implementation

        public event EventHandler<EventLogContext> LogReceived;

        #endregion

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
            var context = new EventLogContext
            {
                Message = formatter(state, exception),
                Timestamp = DateTime.Now,
                Category = this.CategoryName ?? typeof(TState).Name,
                LogLevel = logLevel,
                EventId = eventId,
                Exception = exception,
            };

            try
            {
                if (!this.FilterLog(context)) return;
            }
            catch
            {
                // Suppress any logging errors
                return;
            }

            try
            {
                context.Message = this.FormatLog(context);
            }
            catch
            {
                // Suppress any logging errors
                return;
            }

            LogReceived?.Invoke(this, context);
        }

        #endregion
    }

    public class EventLogger<TCategoryName> : EventLogger, IEventLogger<TCategoryName>
    {
        public EventLogger()
        {
            this.CategoryName = typeof(TCategoryName).Name;
        }
    }
}
