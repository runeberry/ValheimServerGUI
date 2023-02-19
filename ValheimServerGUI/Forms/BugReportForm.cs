using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Logging;

namespace ValheimServerGUI.Forms
{
    public partial class BugReportForm : Form
    {
        private readonly IRuneberryApiClient RuneberryApiClient;

        private readonly IEventLogger Logger;

        public BugReportForm(
            IRuneberryApiClient runeberryApiClient,
            IEventLogger logger)
        {
            RuneberryApiClient = runeberryApiClient;
            Logger = logger;

            InitializeComponent();
            this.AddApplicationIcon();

            ButtonSubmit.Click += this.BuildEventHandler(ButtonSubmit_Click);
            ButtonCancel.Click += this.BuildEventHandler(ButtonCancel_Click);
            BugReportField.ValueChanged += this.BuildEventHandler<string>(BugReportField_ValueChanged);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            ClearForm();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            ClearForm();
        }

        private void ButtonCancel_Click()
        {
            Close();
        }

        private void ButtonSubmit_Click()
        {
            SubmitBugReport();
        }

        private void BugReportField_ValueChanged(string value)
        {
            // Only enable the Submit button when there is some content in the bug report
            ButtonSubmit.Enabled = !string.IsNullOrWhiteSpace(value);
        }

        private void ClearForm()
        {
            BugReportField.Value = string.Empty;
            ContactInfoField.Value = string.Empty;
        }

        private void SubmitBugReport()
        {
            var crashReport = AssemblyHelper.BuildCrashReport();

            var additionalInfo = new Dictionary<string, string>
            {
                { "BugReport", BugReportField.Value },
                { "ContactInfo", ContactInfoField.Value },
            };

            crashReport.Source = "BugReport";
            crashReport.AdditionalInfo = additionalInfo;
            crashReport.Logs = Logger.LogBuffer.Reverse().Take(100).ToList();

            var task = RuneberryApiClient.SendCrashReportAsync(crashReport);
            var asyncPopout = new AsyncPopout(task, o =>
            {
                o.Title = "Bug Report";
                o.Text = "Submitting bug report...";
                o.SuccessMessage = "Bug report submitted. Thank you!";
                o.FailureMessage = "Failed to submit bug report.\r\nContact Runeberry Software for further support.";
            });

            asyncPopout.ShowDialog();

            Close();
        }
    }
}
