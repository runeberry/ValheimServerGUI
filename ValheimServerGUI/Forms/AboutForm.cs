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
            this.AddApplicationIcon();

            this.VersionLabel.Text = $"Version: {AssemblyHelper.GetApplicationVersion()}";
        }

        private void ButtonDonate_Click(object sender, EventArgs e)
        {
            OpenHelper.OpenWebAddress(Resources.UrlDonate);
        }

        private void ButtonGitHub_Click(object sender, EventArgs e)
        {
            OpenHelper.OpenWebAddress(Resources.UrlGithubApplication);
        }

        private void ButtonValheimSite_Click(object sender, EventArgs e)
        {
            OpenHelper.OpenWebAddress(Resources.ValheimGameSiteUrl);
        }
    }
}
