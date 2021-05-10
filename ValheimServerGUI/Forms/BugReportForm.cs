using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.Forms
{
    public partial class BugReportForm : Form
    {
        public BugReportForm()
        {
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
            throw new NotImplementedException();
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
    }
}
