using Serilog.Events;
using System.Text.RegularExpressions;

namespace ValheimServerGUI.Tools.Logging.Components
{
    public class RegexFilter : LogFilter
    {
        private readonly Regex Pattern;
        private readonly bool IncludeMatch;
        private readonly bool MatchAgainstTemplate;

        public RegexFilter(Regex pattern, bool includeMatch, bool matchAgainstTemplate)
        {
            Pattern = pattern;
            IncludeMatch = includeMatch;
            MatchAgainstTemplate = matchAgainstTemplate;
        }

        public RegexFilter(string pattern, bool includeMatch, bool matchAgainstTemplate)
            : this(new Regex(pattern), includeMatch, matchAgainstTemplate) { }

        public static RegexFilter Exclude(string pattern) => new(pattern, false, false);
        public static RegexFilter Include(string pattern) => new(pattern, true, false);
        public static RegexFilter ExcludeTemplate(string pattern) => new(pattern, false, true);
        public static RegexFilter IncludeTemplate(string pattern) => new(pattern, true, true);

        #region LogFilter implementation

        public override bool Include(LogEvent logEvent, string renderedMessage)
        {
            var message = MatchAgainstTemplate ? logEvent.MessageTemplate.Text : renderedMessage;
            var isMatch = Pattern.IsMatch(message);
            var include = IncludeMatch ? isMatch : !isMatch;

            return include;
        }

        #endregion
    }
}
