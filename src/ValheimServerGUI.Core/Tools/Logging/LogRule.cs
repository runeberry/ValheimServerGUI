using Serilog.Events;

namespace ValheimServerGUI.Tools.Logging
{
    public abstract class LogRule
    {
        protected LogRule() { }

        public virtual bool Include(LogEvent logEvent, string renderedMessage)
        {
            return true;
        }

        public virtual string Transform(LogEvent logEvent, string renderedMessage)
        {
            return renderedMessage;
        }
    }

    public abstract class LogFilter : LogRule
    {
        protected LogFilter() { }

        public override abstract bool Include(LogEvent logEvent, string renderedMessage);

        public override sealed string Transform(LogEvent logEvent, string renderedMessage)
        {
            return base.Transform(logEvent, renderedMessage);
        }
    }

    public abstract class LogTransformer : LogRule
    {
        protected LogTransformer() { }

        public override sealed bool Include(LogEvent logEvent, string renderedMessage)
        {
            return base.Include(logEvent, renderedMessage);
        }

        public override abstract string Transform(LogEvent logEvent, string renderedMessage);
    }
}
