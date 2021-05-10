using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ValheimServerGUI.Forms;

namespace ValheimServerGUI.Tools
{
    public interface IExceptionHandler
    {
        event EventHandler ExceptionHandled;

        void HandleException(Exception e, string contextMessage = null);
    }

    public class ExceptionHandler : IExceptionHandler
    {
        private readonly IRuneberryApiClient RuneberryApiClient;

        public ExceptionHandler(IRuneberryApiClient runeberryApiClient)
        {
            RuneberryApiClient = runeberryApiClient;
        }

        public event EventHandler ExceptionHandled;

        public void HandleException(Exception e, string contextMessage = null)
        {
            if (e == null) return;

            e = e.GetPrimaryException();

            contextMessage ??= "Unknown Exception";
            var userMessage = "A fatal error has occured. Would you like to send an automated crash report to the developer?";

            var result = MessageBox.Show(
                userMessage,
                contextMessage,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Error);

            if (result == DialogResult.Yes)
            {
                var crashReport = BuildCrashReport(e, contextMessage);
                var task = RuneberryApiClient.SendCrashReportAsync(crashReport);

                var asyncPopout = new AsyncPopout(task, o =>
                {
                    o.Title = "Crash Report";
                    o.Text = "Sending crash report...";
                    o.SuccessMessage = "Crash report received. Thank you!";
                    o.FailureMessage = "Failed to send crash report.\r\nContact Runeberry Software for further support.";
                });

                asyncPopout.ShowDialog();
            }

            this.ExceptionHandled?.Invoke(this, EventArgs.Empty);
        }

        private CrashReport BuildCrashReport(Exception e, string contextMessage)
        {
            var crashReport = AssemblyHelper.BuildCrashReport();

            var additionalInfo = new Dictionary<string, string>
            {
                { "ExceptionType", e.GetType().Name },
                { "Message", e.Message },
                { "Context", contextMessage },
                { "Source", e.Source },
                { "TargetSite", e.TargetSite?.ToString() },
                { "StackTrace", e.StackTrace },
            };

            crashReport.Source = "CrashReport";
            crashReport.AdditionalInfo = additionalInfo;

            return crashReport;
        }
    }
}
