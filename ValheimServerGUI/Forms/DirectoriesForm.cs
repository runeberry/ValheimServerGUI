using System;
using System.Windows.Forms;
using ValheimServerGUI.Game;

namespace ValheimServerGUI.Forms
{
    public partial class DirectoriesForm : Form
    {
        private readonly IUserPreferencesProvider UserPrefsProvider;

        private readonly IValheimFileProvider FileProvider;

        public DirectoriesForm()
        {
            InitializeComponent();
        }

        public DirectoriesForm(IUserPreferencesProvider userPrefsProvider, IValheimFileProvider fileProvider) : this()
        {
            this.UserPrefsProvider = userPrefsProvider;
            this.FileProvider = fileProvider;
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
            if (this.SavePreferences())
            {
                this.Close();
            }
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ButtonDefaults_Click(object sender, EventArgs e)
        {
            this.RestoreDefaults();
        }

        private bool SavePreferences()
        {
            var prefs = this.UserPrefsProvider.LoadPreferences();

            var (pGamePath, pServerPath, pSaveDataFolder) = (prefs.ValheimGamePath, prefs.ValheimServerPath, prefs.ValheimSaveDataFolder);

            prefs.ValheimGamePath = this.GamePathField.Value;
            prefs.ValheimServerPath = this.ServerPathField.Value;
            prefs.ValheimSaveDataFolder = this.SaveDataFolderField.Value;
                        
            try
            {
                // Validate folders by ensuring that these properties can be called
                _ = FileProvider.GameExe;
                _ = FileProvider.ServerExe;
                _ = FileProvider.SaveDataFolder;
            }
            catch (Exception e)
            {
                var dialogResult = MessageBox.Show(
                    $"{e.Message}\r\n\r\nSave anyway?",
                    e.GetType().Name,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (dialogResult == DialogResult.No)
                {
                    // Revert the preference updates if the user chooses to back out
                    prefs.ValheimGamePath = pGamePath;
                    prefs.ValheimServerPath = pServerPath;
                    prefs.ValheimSaveDataFolder = pSaveDataFolder;

                    return false;
                }
            }
            
            this.UserPrefsProvider.SavePreferences(prefs);
            return true;
        }

        private void RestoreDefaults()
        {
            this.GamePathField.Value = Environment.ExpandEnvironmentVariables(UserPreferences.Default.ValheimGamePath);
            this.ServerPathField.Value = Environment.ExpandEnvironmentVariables(UserPreferences.Default.ValheimServerPath);
            this.SaveDataFolderField.Value = Environment.ExpandEnvironmentVariables(UserPreferences.Default.ValheimSaveDataFolder);
        }
    }
}
