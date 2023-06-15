using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Text.RegularExpressions;

namespace ValheimServerGUI.Tools.Logging.Components
{
    public class RegexFilter : ILogEventFilter
    {
        private readonly string Pattern;
        private readonly bool Include;

        public RegexFilter(string pattern, bool include)
        {
            if (string.IsNullOrEmpty(pattern)) throw new ArgumentNullException(nameof(pattern));

            Pattern = pattern;
            Include = include;
        }

        public bool IsEnabled(LogEvent logEvent)
        {
            var isMatch = Regex.IsMatch(logEvent.MessageTemplate.Text, Pattern);

            return Include ? isMatch : !isMatch;
        }
    }

    public static class RegexFilterExtensions
    {
        /// <summary>
        /// Only include logs that match the specified pattern.
        /// </summary>
        public static LoggerConfiguration AddRegexInclusion(this LoggerConfiguration config, string pattern)
        {
            return config.Filter.With(new RegexFilter(pattern, true));
        }

        /// <summary>
        /// Exclude logs that match the specified pattern.
        /// </summary>
        public static LoggerConfiguration AddRegexExclusion(this LoggerConfiguration config, string pattern)
        {
            return config.Filter.With(new RegexFilter(pattern, false));
        }
    }
}
