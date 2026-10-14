using Serilog.Events;
using System.Text.RegularExpressions;

namespace ValheimServerGUI.Tools.Logging.Components
{
    public class RegexTransformer : LogTransformer
    {
        private readonly string CapturePattern;
        private readonly string ReplacePattern;

        public RegexTransformer(string capture, string replace)
        {
            CapturePattern = capture;
            ReplacePattern = replace;
        }

        public static RegexTransformer Replace(string capture, string replace) => new(capture, replace);
        public static RegexTransformer Remove(string pattern) => new(pattern, string.Empty);

        #region LogTransformer implementation

        public override string Transform(LogEvent logEvent, string renderedMessage)
        {
            return Regex.Replace(renderedMessage, CapturePattern, ReplacePattern);
        }

        #endregion
    }
}
