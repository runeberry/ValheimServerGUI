using System;
using System.Windows.Forms;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.Forms
{
    public partial class DirectoriesForm : Form
    {
        private readonly IUserPreferencesProvider UserPrefsProvider;

        public DirectoriesForm()
        {
            InitializeComponent();
            this.AddApplicationIcon();
        }

        public DirectoriesForm(IUserPreferencesProvider userPrefsProvider) : this()
        {
            UserPrefsProvider = userPrefsProvider;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            InitializeFormFields();
        }

        private void InitializeFormFields()
        {
            var prefs = UserPrefsProvider.LoadPreferences();

            ServerExePathField.Value = prefs.ServerExePath;
            SaveDataFolderPathField.Value = prefs.SaveDataFolderPath;

            ServerExePathField.ConfigureFileDialog(dialog => dialog.Filter = "Applications (*.exe)|*.exe");
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

            var (pServerPath, pSaveDataFolder) = (prefs.ServerExePath, prefs.SaveDataFolderPath);

            prefs.ServerExePath = ServerExePathField.Value;
            prefs.SaveDataFolderPath = SaveDataFolderPathField.Value;

            try
            {
                // Validate folders by ensuring that these methods can be called
                var options = new ValheimServerOptions
                {
                    ServerExePath = prefs.ServerExePath,
                    SaveDataFolderPath = prefs.SaveDataFolderPath,
                };

                options.GetValidatedServerExe();
                options.GetValidatedSaveDataFolder();
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
                    prefs.ServerExePath = pServerPath;
                    prefs.SaveDataFolderPath = pSaveDataFolder;

                    return false;
                }
            }

            UserPrefsProvider.SavePreferences(prefs);
            return true;
        }

        private void RestoreDefaults()
        {
            var prefs = UserPreferences.GetDefault();

            ServerExePathField.Value = prefs.ServerExePath;
            SaveDataFolderPathField.Value = prefs.SaveDataFolderPath;
        }
    }
}
