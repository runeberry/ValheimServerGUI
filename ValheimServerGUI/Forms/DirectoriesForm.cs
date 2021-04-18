using System;
using System.Windows.Forms;
using ValheimServerGUI.Game;

namespace ValheimServerGUI.Forms
{
    public partial class DirectoriesForm : Form
    {
        private readonly IUserPreferencesProvider UserPrefsProvider;

        public DirectoriesForm()
        {
            InitializeComponent();
        }

        public DirectoriesForm(IUserPreferencesProvider userPrefsProvider) : this()
        {
            this.UserPrefsProvider = userPrefsProvider;

            InitializeFormFields();
        }

        private void InitializeFormFields()
        {
            var prefs = this.UserPrefsProvider.LoadPreferences();

            this.GamePathField.Value = Environment.ExpandEnvironmentVariables(prefs.ValheimGamePath);
            this.ServerPathField.Value = Environment.ExpandEnvironmentVariables(prefs.ValheimServerPath);
            this.WorldsFolderField.Value = Environment.ExpandEnvironmentVariables(prefs.ValheimWorldsFolder);

            this.GamePathField.ConfigureFileDialog(dialog => dialog.Filter = "Applications (*.exe)|*.exe");
            this.ServerPathField.ConfigureFileDialog(dialog => dialog.Filter = "Applications (*.exe)|*.exe");

            // Currently valheim_server doesn't support using different world folders.
            // Re-enable this control and properly add support in the server options if a method ever gets added to do this.
            this.WorldsFolderField.ReadOnly = true;
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            var prefs = this.UserPrefsProvider.LoadPreferences();

            prefs.ValheimGamePath = this.GamePathField.Value;
            prefs.ValheimServerPath = this.ServerPathField.Value;
            prefs.ValheimWorldsFolder = this.WorldsFolderField.Value;

            this.UserPrefsProvider.SavePreferences(prefs);
            this.Close();
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ButtonDefaults_Click(object sender, EventArgs e)
        {
            this.GamePathField.Value = Environment.ExpandEnvironmentVariables(UserPreferences.Default.ValheimGamePath);
            this.ServerPathField.Value = Environment.ExpandEnvironmentVariables(UserPreferences.Default.ValheimServerPath);
            this.WorldsFolderField.Value = Environment.ExpandEnvironmentVariables(UserPreferences.Default.ValheimWorldsFolder);
        }
    }
}
