using System;
using System.Windows.Forms;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.Forms
{
    public partial class AdvancedServerControlsForm : Form
    {
        private readonly IUserPreferencesProvider UserPrefsProvider;

        public AdvancedServerControlsForm()
        {
            InitializeComponent();
            this.AddApplicationIcon();
        }

        public AdvancedServerControlsForm(IUserPreferencesProvider userPrefsProvider) : this()
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

            this.SaveIntervalField.Value = prefs.ServerSaveInterval;
            this.BackupsField.Value = prefs.ServerBackupCount;
            this.ShortBackupIntervalField.Value = prefs.ServerBackupIntervalShort;
            this.LongBackupIntervalField.Value = prefs.ServerBackupIntervalLong;
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            var prefs = this.UserPrefsProvider.LoadPreferences();

            prefs.ServerSaveInterval = this.SaveIntervalField.Value;
            prefs.ServerBackupCount = this.BackupsField.Value;
            prefs.ServerBackupIntervalShort = this.ShortBackupIntervalField.Value;
            prefs.ServerBackupIntervalLong = this.LongBackupIntervalField.Value;

            var err = ValidatePrefs(prefs);
            if (err != null)
            {
                MessageBox.Show(
                    err,
                    "Invalid Settings",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            this.UserPrefsProvider.SavePreferences(prefs);
            this.Close();
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ButtonDefaults_Click(object sender, EventArgs e)
        {
            var prefs = UserPreferences.GetDefault();

            this.SaveIntervalField.Value = prefs.ServerSaveInterval;
            this.BackupsField.Value = prefs.ServerBackupCount;
            this.ShortBackupIntervalField.Value = prefs.ServerBackupIntervalShort;
            this.LongBackupIntervalField.Value = prefs.ServerBackupIntervalLong;
        }

        private static string ValidatePrefs(UserPreferences prefs)
        {
            if (prefs.ServerSaveInterval > prefs.ServerBackupIntervalShort ||
                prefs.ServerSaveInterval > prefs.ServerBackupIntervalLong)
            {
                return "The save interval must be shorter than the backup intervals.";
            }

            if (prefs.ServerBackupIntervalShort > prefs.ServerBackupIntervalLong)
            {
                return "The short backup interval must be shorter than the long backup interval.";
            }

            return null;
        }
    }
}
