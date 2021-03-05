using System;
using System.Windows.Forms;
using ValheimServerGUI.Tools.Preferences;

namespace ValheimServerGUI.Forms
{
    public partial class DirectoriesForm : Form
    {
        private readonly IUserPreferences UserPrefs;

        public DirectoriesForm()
        {
            InitializeComponent();
        }

        public DirectoriesForm(IUserPreferences userPrefs) : this()
        {
            this.UserPrefs = userPrefs;

            InitializeFormFields();
        }

        private void InitializeFormFields()
        {
            this.GamePathField.Value = this.UserPrefs.GetEnvironmentValue(PrefKeys.ValheimGamePath);
            this.ServerPathField.Value = this.UserPrefs.GetEnvironmentValue(PrefKeys.ValheimServerPath);
            this.WorldsFolderField.Value = this.UserPrefs.GetEnvironmentValue(PrefKeys.ValheimWorldsFolder);

            this.GamePathField.ConfigureFileDialog(dialog => dialog.Filter = "Applications (*.exe)|*.exe");
            this.ServerPathField.ConfigureFileDialog(dialog => dialog.Filter = "Applications (*.exe)|*.exe");
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            this.UserPrefs.SetValue(PrefKeys.ValheimGamePath, this.GamePathField.Value);
            this.UserPrefs.SetValue(PrefKeys.ValheimServerPath, this.ServerPathField.Value);
            this.UserPrefs.SetValue(PrefKeys.ValheimWorldsFolder, this.WorldsFolderField.Value);
            this.UserPrefs.SaveFile();

            this.Close();
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ButtonDefaults_Click(object sender, EventArgs e)
        {
            this.GamePathField.Value = UserPreferences.Default.GetEnvironmentValue(PrefKeys.ValheimGamePath);
            this.ServerPathField.Value = UserPreferences.Default.GetEnvironmentValue(PrefKeys.ValheimServerPath);
            this.WorldsFolderField.Value = UserPreferences.Default.GetEnvironmentValue(PrefKeys.ValheimWorldsFolder);
        }
    }
}
