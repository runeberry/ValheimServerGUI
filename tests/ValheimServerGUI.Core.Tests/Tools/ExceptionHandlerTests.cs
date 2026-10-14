using System;
using ValheimServerGUI.Core.Tests.Fakes;
using ValheimServerGUI.Tools;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Tools
{
    /// <summary>
    /// The exception handler builds a crash report and sends it only with the user's consent, through
    /// the IUserPrompt seam (formerly a WinForms MessageBox). ExceptionHandled fires either way.
    /// </summary>
    public class ExceptionHandlerTests
    {
        private static (ExceptionHandler handler, FakeRuneberryApiClient runeberry, FakeUserPrompt prompt, FakeApplicationLogger logger) Build(bool consent)
        {
            var runeberry = new FakeRuneberryApiClient();
            var prompt = new FakeUserPrompt(answer: consent);
            var logger = new FakeApplicationLogger();
            var handler = new ExceptionHandler(runeberry, logger, prompt);
            return (handler, runeberry, prompt, logger);
        }

        [Fact]
        public void HandleException_WithConsent_SendsReport()
        {
            var (handler, runeberry, prompt, _) = Build(consent: true);
            var handled = false;
            handler.ExceptionHandled += (_, _) => handled = true;

            handler.HandleException(new InvalidOperationException("boom"), "Test Context");

            Assert.Single(prompt.Confirmations);
            Assert.Single(runeberry.SentReports);
            Assert.Equal("CrashReport", runeberry.SentReports[0].Source);
            Assert.True(handled);
        }

        [Fact]
        public void HandleException_WithoutConsent_DoesNotSend()
        {
            var (handler, runeberry, prompt, _) = Build(consent: false);
            var handled = false;
            handler.ExceptionHandled += (_, _) => handled = true;

            handler.HandleException(new InvalidOperationException("boom"));

            Assert.Single(prompt.Confirmations);
            Assert.Empty(runeberry.SentReports);
            Assert.True(handled);
        }

        // Every fault is logged before the crash-report prompt (WinForms parity — SplashForm.HandleException).
        [Fact]
        public void HandleException_LogsTheFault()
        {
            var (handler, _, _, logger) = Build(consent: false);

            handler.HandleException(new InvalidOperationException("boom"), "Test Context");

            Assert.Contains(logger.Messages, m => m.Contains("Encountered exception") && m.Contains("boom"));
        }

        // A cancelled task/operation reaching a global handler is benign: it is logged, but never surfaces the
        // fatal crash-report prompt (regression: this used to pop a fatal "A task was canceled" dialog).
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void HandleException_CancellationIsLoggedButNotFatal(bool wrappedInAggregate)
        {
            var (handler, runeberry, prompt, logger) = Build(consent: true);
            var handled = false;
            handler.ExceptionHandled += (_, _) => handled = true;

            Exception ex = new System.Threading.Tasks.TaskCanceledException();
            if (wrappedInAggregate) ex = new AggregateException(ex);

            handler.HandleException(ex, "Unobserved task exception");

            Assert.Empty(prompt.Confirmations);       // no fatal prompt
            Assert.Empty(runeberry.SentReports);      // no crash report
            Assert.False(handled);                    // ExceptionHandled not raised
            Assert.Contains(logger.Messages, m => m.Contains("cancelled"));
        }

        // An AggregateException unwraps to its primary inner exception.
        [Fact]
        public void HandleException_UnwrapsAggregate()
        {
            var (handler, runeberry, _, _) = Build(consent: true);
            var inner = new InvalidOperationException("real cause");

            handler.HandleException(new AggregateException(inner));

            Assert.Single(runeberry.SentReports);
            Assert.Equal(nameof(InvalidOperationException), runeberry.SentReports[0].AdditionalInfo!["ExceptionType"]);
        }
    }
}
