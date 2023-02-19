using System;
using System.Windows.Forms;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.Forms
{
    public partial class DirectoriesForm : Form
    {
        private readonly IUserPreferencesProvider UserPrefsProvider;

        private readonly IValheimFileProvider FileProvider;

        public DirectoriesForm()
        {
            InitializeComponent();
            this.AddApplicationIcon();
        }

        public DirectoriesForm(IUserPreferencesProvider userPrefsProvider, IValheimFileProvider fileProvider) : this()
        {
            UserPrefsProvider = userPrefsProvider;
            FileProvider = fileProvider;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            InitializeFormFields();
        }

        private void InitializeFormFields()
        {
            var prefs = UserPrefsProvider.LoadPreferences();

            ServerPathField.Value = Environment.ExpandEnvironmentVariables(prefs.ValheimServerPath);
            SaveDataFolderField.Value = Environment.ExpandEnvironmentVariables(prefs.ValheimSaveDataFolder);

            ServerPathField.ConfigureFileDialog(dialog => dialog.Filter = "Applications (*.exe)|*.exe");
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            if (SavePreferences())
            {
                Close();
            }
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ButtonDefaults_Click(object sender, EventArgs e)
        {
            RestoreDefaults();
        }

        private bool SavePreferences()
        {
            var prefs = UserPrefsProvider.LoadPreferences();

            var (pServerPath, pSaveDataFolder) = (prefs.ValheimServerPath, prefs.ValheimSaveDataFolder);

            prefs.ValheimServerPath = ServerPathField.Value;
            prefs.ValheimSaveDataFolder = SaveDataFolderField.Value;

            try
            {
                // Validate folders by ensuring that these properties can be called
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
                    prefs.ValheimServerPath = pServerPath;
                    prefs.ValheimSaveDataFolder = pSaveDataFolder;

                    return false;
                }
            }

            UserPrefsProvider.SavePreferences(prefs);
            return true;
        }

        private void RestoreDefaults()
        {
            var prefs = UserPreferences.GetDefault();

            ServerPathField.Value = Environment.ExpandEnvironmentVariables(prefs.ValheimServerPath);
            SaveDataFolderField.Value = Environment.ExpandEnvironmentVariables(prefs.ValheimSaveDataFolder);
        }
    }
}
