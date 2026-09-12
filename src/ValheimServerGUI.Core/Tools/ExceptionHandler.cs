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

        public void HandleException(Exception e, string? contextMessage = null)
        {
            if (e == null) return;

            e = e.GetPrimaryException();

            contextMessage ??= "Unknown Exception";
            var userMessage = $"A fatal error has occured: {e.Message}{Environment.NewLine}{Environment.NewLine}Would you like to send an automated crash report to the developer?";

            if (UserPrompt.Confirm(userMessage, contextMessage))
            {
                var crashReport = BuildCrashReport(e, contextMessage);
                _ = SendCrashReportSafelyAsync(crashReport);
            }

            ExceptionHandled?.Invoke(this, EventArgs.Empty);
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
