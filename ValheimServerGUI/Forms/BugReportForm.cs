using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.Forms
{
    public partial class BugReportForm : Form
    {
        private readonly IRuneberryApiClient RuneberryApiClient;
        
        public BugReportForm(IRuneberryApiClient runeberryApiClient)
        {
            this.RuneberryApiClient = runeberryApiClient;

            InitializeComponent();
            this.AddApplicationIcon();

            this.ButtonSubmit.Click += this.BuildEventHandler(this.ButtonSubmit_Click);
            this.ButtonCancel.Click += this.BuildEventHandler(this.ButtonCancel_Click);
            this.BugReportField.ValueChanged += this.BuildEventHandler<string>(this.BugReportField_ValueChanged);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            this.ClearForm();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            this.ClearForm();
        }

        private void ButtonCancel_Click()
        {
            this.Close();
        }

        private void ButtonSubmit_Click()
        {
            this.SubmitBugReport();
        }

        private void BugReportField_ValueChanged(string value)
        {
            // Only enable the Submit button when there is some content in the bug report
            this.ButtonSubmit.Enabled = !string.IsNullOrWhiteSpace(value);
        }

        private void ClearForm()
        {
            this.BugReportField.Value = string.Empty;
            this.ContactInfoField.Value = string.Empty;
        }

        private void SubmitBugReport()
        {
            var crashReport = AssemblyHelper.BuildCrashReport();

            var additionalInfo = new Dictionary<string, string>
            {
                { "BugReport", this.BugReportField.Value },
                { "ContactInfo", this.ContactInfoField.Value },
            };

            crashReport.Source = "BugReport";
            crashReport.AdditionalInfo = additionalInfo;

            var task = RuneberryApiClient.SendCrashReportAsync(crashReport);
            var asyncPopout = new AsyncPopout(task, o =>
            {
                o.Title = "Bug Report";
                o.Text = "Submitting bug report...";
                o.SuccessMessage = "Bug report submitted. Thank you!";
                o.FailureMessage = "Failed to submit bug report.\r\nContact Runeberry Software for further support.";
            });

            asyncPopout.ShowDialog();

            this.Close();
        }
    }
}
