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
        private static (ExceptionHandler handler, FakeRuneberryApiClient runeberry, FakeUserPrompt prompt) Build(bool consent)
        {
            var runeberry = new FakeRuneberryApiClient();
            var prompt = new FakeUserPrompt(answer: consent);
            var handler = new ExceptionHandler(runeberry, new FakeApplicationLogger(), prompt);
            return (handler, runeberry, prompt);
        }

        [Fact]
        public void HandleException_WithConsent_SendsReport()
        {
            var (handler, runeberry, prompt) = Build(consent: true);
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
            var (handler, runeberry, prompt) = Build(consent: false);
            var handled = false;
            handler.ExceptionHandled += (_, _) => handled = true;

            handler.HandleException(new InvalidOperationException("boom"));

            Assert.Single(prompt.Confirmations);
            Assert.Empty(runeberry.SentReports);
            Assert.True(handled);
        }

        // An AggregateException unwraps to its primary inner exception.
        [Fact]
        public void HandleException_UnwrapsAggregate()
        {
            var (handler, runeberry, _) = Build(consent: true);
            var inner = new InvalidOperationException("real cause");

            handler.HandleException(new AggregateException(inner));

            Assert.Single(runeberry.SentReports);
            Assert.Equal(nameof(InvalidOperationException), runeberry.SentReports[0].AdditionalInfo!["ExceptionType"]);
        }
    }
}
