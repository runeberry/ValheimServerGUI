using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ValheimServerGUI.Tools.Logging;
using ValheimServerGUI.Tools.Models;

namespace ValheimServerGUI.Tools
{
    public interface IExceptionHandler
    {
        event EventHandler ExceptionHandled;

        void HandleException(Exception e, string? contextMessage = null);
    }

    /// <summary>
    /// Builds a crash report from an exception and, with the user's consent, sends it to the Runeberry
    /// backend. The consent prompt is the <see cref="IUserPrompt"/> seam (formerly a WinForms MessageBox);
    /// the progress dialog that the WinForms shell showed while sending is a Phase 2 shell concern.
    /// </summary>
    public class ExceptionHandler : IExceptionHandler
    {
        private readonly IRuneberryApiClient RuneberryApiClient;
        private readonly IApplicationLogger Logger;
        private readonly IUserPrompt UserPrompt;

        public ExceptionHandler(IRuneberryApiClient runeberryApiClient, IApplicationLogger logger, IUserPrompt userPrompt)
        {
            RuneberryApiClient = runeberryApiClient;
            Logger = logger;
            UserPrompt = userPrompt;
        }

        public event EventHandler? ExceptionHandled;

        /// <summary>
        /// A cancelled task/operation is benign — it reaches a global handler on teardown or a request
        /// timeout, not from a real crash. We log it, but never surface the fatal crash-report prompt for it.
        /// </summary>
        internal static bool IsBenignCancellation(Exception e) => e is OperationCanceledException;

        public void HandleException(Exception e, string? contextMessage = null)
        {
            if (e == null) return;

            e = e.GetPrimaryException();
            contextMessage ??= "Unknown Exception";

            if (IsBenignCancellation(e))
            {
                // Log-only: an operation was cancelled (e.g. on shutdown), which is not a crash.
                TryLog(() => Logger.Debug("Operation cancelled ({context}): {message}", contextMessage, e.Message));
                return;
            }

            // Always log the fault first (WinForms parity — SplashForm.HandleException), guarded so a logging
            // failure can never take the process down on the global exception path.
            TryLog(() => Logger.Error(
                "Encountered exception ({context}) - {typeName}: {message}{newline}{stackTrace}",
                contextMessage, e.GetType().Name, e.Message, Environment.NewLine, e.StackTrace ?? string.Empty));

            var userMessage = $"A fatal error has occured: {e.Message}{Environment.NewLine}{Environment.NewLine}Would you like to send an automated crash report to the developer?";

            if (UserPrompt.Confirm(userMessage, contextMessage))
            {
                var crashReport = BuildCrashReport(e, contextMessage);
                _ = SendCrashReportSafelyAsync(crashReport);
            }

            ExceptionHandled?.Invoke(this, EventArgs.Empty);
        }

        private void TryLog(Action log)
        {
            try { log(); }
            catch { /* never let logging itself fail the exception handler */ }
        }

        private async Task SendCrashReportSafelyAsync(CrashReport crashReport)
        {
            try
            {
                await RuneberryApiClient.SendCrashReportAsync(crashReport);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to send crash report");
            }
        }

        private CrashReport BuildCrashReport(Exception e, string contextMessage)
        {
            var crashReport = AssemblyHelper.BuildCrashReport();

            var additionalInfo = new Dictionary<string, string>
            {
                { "ExceptionType", e.GetType().Name },
                { "Message", e.Message },
                { "Context", contextMessage },
                { "Source", e.Source ?? string.Empty },
                { "TargetSite", e.TargetSite?.ToString() ?? string.Empty },
                { "StackTrace", e.StackTrace ?? string.Empty },
            };

            crashReport.Source = "CrashReport";
            crashReport.AdditionalInfo = additionalInfo;
            crashReport.Logs = Logger.LogBuffer.Reverse().Take(100).ToList();

            return crashReport;
        }
    }
}
