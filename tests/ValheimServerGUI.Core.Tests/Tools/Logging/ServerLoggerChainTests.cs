using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Serilog;
using Serilog.Events;
using Serilog.Parsing;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools.Logging;
using ValheimServerGUI.Tools.Logging.Components;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Tools.Logging
{
    /// <summary>
    /// Pins the fragile server log-rule chain (E48, §5.5) and the level tagging (E49). The chain runs
    /// in order: strip any native timestamp, drop Unity noise / console / blank lines (filtering on),
    /// then prepend the app's own timestamp.
    /// </summary>
    public class ServerLoggerChainTests
    {
        private static readonly Regex VsgTimestamp = new(@"^\[\d\d:\d\d:\d\d\.\d\d\d\] ");

        private static (ValheimServerLogger logger, List<string> captured) BuildLogger(bool filteringDisabled = false)
        {
            var options = new ValheimServerOptions
            {
                Name = "Test",
                LogFilteringDisabled = filteringDisabled,
                LogToFile = false,
            };
            var resolver = new LinuxValheimPathResolver("/tmp/vsg-test-home", xdgDataHome: null);
            var logger = new ValheimServerLogger(options, resolver);

            var captured = new List<string>();
            logger.LogReceived += captured.Add;
            return (logger, captured);
        }

        [Theory]
        [InlineData("(Filename: ./foo/bar.cpp Line: 42)")]
        [InlineData("Console: something noisy")]
        [InlineData("   ")]
        public void FilteringOn_DropsUnityNoiseConsoleAndBlank(string line)
        {
            var (logger, captured) = BuildLogger(filteringDisabled: false);

            ((ILogger)logger).Information(line);

            Assert.Empty(captured);
        }

        [Fact]
        public void FilteringOn_KeepsRealLine_AndPrependsAppTimestamp()
        {
            var (logger, captured) = BuildLogger(filteringDisabled: false);

            ((ILogger)logger).Information("World saved successfully");

            var message = Assert.Single(captured);
            Assert.Matches(VsgTimestamp, message);
            Assert.EndsWith("World saved successfully", message);
        }

        [Fact]
        public void Chain_StripsNativeTimestamp_BeforePrependingAppTimestamp()
        {
            var (logger, captured) = BuildLogger(filteringDisabled: false);

            ((ILogger)logger).Information("01/02/2034 12:34:56: Actual message");

            var message = Assert.Single(captured);
            Assert.DoesNotContain("2034", message);
            Assert.Matches(VsgTimestamp, message);
            Assert.EndsWith("Actual message", message);
        }

        [Fact]
        public void FilteringDisabled_KeepsUnityNoise()
        {
            var (logger, captured) = BuildLogger(filteringDisabled: true);

            ((ILogger)logger).Information("(Filename: ./foo Line: 1)");

            Assert.Single(captured);
        }

        // E49: the level transformer tags everything except Information.
        [Theory]
        [InlineData(LogEventLevel.Information, "hello", "hello")]
        [InlineData(LogEventLevel.Warning, "hello", "[WRN] hello")]
        [InlineData(LogEventLevel.Error, "hello", "[ERR] hello")]
        [InlineData(LogEventLevel.Debug, "hello", "[DBG] hello")]
        public void LogLevelTransformer_TagsAllButInformation(LogEventLevel level, string input, string expected)
        {
            var logEvent = new LogEvent(
                DateTimeOffset.Now, level, exception: null,
                new MessageTemplateParser().Parse(input),
                Array.Empty<LogEventProperty>());

            Assert.Equal(expected, LogLevelTransformer.Default.Transform(logEvent, input));
        }
    }
}
