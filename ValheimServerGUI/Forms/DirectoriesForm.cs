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
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            InitializeFormFields();
        }

        private void InitializeFormFields()
        {
            var prefs = this.UserPrefsProvider.LoadPreferences();

            this.GamePathField.Value = Environment.ExpandEnvironmentVariables(prefs.ValheimGamePath);
            this.ServerPathField.Value = Environment.ExpandEnvironmentVariables(prefs.ValheimServerPath);
            this.SaveDataFolderField.Value = Environment.ExpandEnvironmentVariables(prefs.ValheimSaveDataFolder);

            this.GamePathField.ConfigureFileDialog(dialog => dialog.Filter = "Applications (*.exe)|*.exe");
            this.ServerPathField.ConfigureFileDialog(dialog => dialog.Filter = "Applications (*.exe)|*.exe");
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            var prefs = this.UserPrefsProvider.LoadPreferences();

            prefs.ValheimGamePath = this.GamePathField.Value;
            prefs.ValheimServerPath = this.ServerPathField.Value;
            prefs.ValheimSaveDataFolder = this.SaveDataFolderField.Value;

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
            this.SaveDataFolderField.Value = Environment.ExpandEnvironmentVariables(UserPreferences.Default.ValheimSaveDataFolder);
        }
    }
}
