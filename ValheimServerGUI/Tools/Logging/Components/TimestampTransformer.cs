using Serilog.Events;

namespace ValheimServerGUI.Tools.Logging.Components
{
    public class TimestampTransformer : LogTransformer
    {
        private readonly string Format;
        private readonly string Space;

        public TimestampTransformer(string format, int space = 0)
        {
            Format = format;
            Space = space <= 0 ? string.Empty : new(' ', space);
        }

        public static readonly TimestampTransformer Default = new(TimeFormats.LogPrefix, 1);

        public override string Transform(LogEvent logEvent, string renderedMessage)
        {
            return $"{logEvent.Timestamp.ToString(Format)}{Space}{renderedMessage}";
        }
    }
}
