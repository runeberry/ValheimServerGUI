using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ValheimServerGUI.Forms;

namespace ValheimServerGUI.Tools
{
    public interface IExceptionHandler
    {
        void HandleException(Exception e, string additionalMessage = null);
    }

    public class ExceptionHandler : IExceptionHandler
    {
        private readonly IRuneberryApiClient RuneberryApiClient;

        public ExceptionHandler(IRuneberryApiClient runeberryApiClient)
        {
            RuneberryApiClient = runeberryApiClient;
        }

        public void HandleException(Exception e, string additionalMessage = null)
        {
            if (e == null) return;
            
            additionalMessage ??= "Unhandled Exception";
            
            var userMessage = "A fatal error has occured. Would you like to send an automated crash report to the developer?";

            var result = MessageBox.Show(
                userMessage,
                additionalMessage,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Error);

            if (result == DialogResult.Yes)
            {
                var crashReport = BuildCrashReport(e, additionalMessage);
                var task = RuneberryApiClient.SendCrashReportAsync(crashReport);

                var asyncPopout = new AsyncPopout(task, options =>
                {
                    options.Title = "Crash Report";
                    options.Text = "Sending crash report...";
                    options.CloseOnSuccess = true;
                    options.SuccessMessage = "Crash report received. Thank you!";
                    options.FailureMessage = "Failed to send crash report.\r\nContact Runeberry Software for further support.";
                });

                // todo: this fails if the application is already running
                Application.Run(asyncPopout);
            }
        }

        private CrashReport BuildCrashReport(Exception e, string additionalMessage)
        {
            var additionalInfo = new Dictionary<string, string>
            {
                { "ExceptionType", e.GetType().Name },
                { "Message", e.Message },
                { "Context", additionalMessage },
                { "Source", e.Source },
                { "TargetSite", e.TargetSite?.ToString() },
                { "StackTrace", e.StackTrace },
            };

            return new CrashReport
            {
                CrashReportId = Guid.NewGuid().ToString(),
                Source = "UnhandledException",
                Timestamp = DateTime.UtcNow,
                AppVersion = AssemblyHelper.GetApplicationVersion(),
                OsVersion = Environment.OSVersion.VersionString,
                DotnetVersion = Environment.Version.ToString(),
                AdditionalInfo = additionalInfo,
            };
        }
    }
}
