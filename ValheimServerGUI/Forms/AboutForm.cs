using System;
using System.Windows.Forms;
using ValheimServerGUI.Properties;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.Forms
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();

            this.VersionLabel.Text = $"Version: {AssemblyHelper.GetApplicationVersion()}";
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            WebHelper.OpenWebAddress(Resources.ApplicationGithubUrl);
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            WebHelper.OpenWebAddress(Resources.ValheimGameSiteUrl);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
