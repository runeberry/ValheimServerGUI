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

            this.SaveIntervalField.Value = prefs.Servers[0].SaveInterval;
            this.BackupsField.Value = prefs.Servers[0].BackupCount;
            this.ShortBackupIntervalField.Value = prefs.Servers[0].BackupIntervalShort;
            this.LongBackupIntervalField.Value = prefs.Servers[0].BackupIntervalLong;
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            var prefs = this.UserPrefsProvider.LoadPreferences();

            prefs.Servers[0].SaveInterval = this.SaveIntervalField.Value;
            prefs.Servers[0].BackupCount = this.BackupsField.Value;
            prefs.Servers[0].BackupIntervalShort = this.ShortBackupIntervalField.Value;
            prefs.Servers[0].BackupIntervalLong = this.LongBackupIntervalField.Value;

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

            this.SaveIntervalField.Value = prefs.Servers[0].SaveInterval;
            this.BackupsField.Value = prefs.Servers[0].BackupCount;
            this.ShortBackupIntervalField.Value = prefs.Servers[0].BackupIntervalShort;
            this.LongBackupIntervalField.Value = prefs.Servers[0].BackupIntervalLong;
        }

        private static string ValidatePrefs(UserPreferences prefs)
        {
            if (prefs.Servers[0].SaveInterval > prefs.Servers[0].BackupIntervalShort ||
                prefs.Servers[0].SaveInterval > prefs.Servers[0].BackupIntervalLong)
            {
                return "The save interval must be shorter than the backup intervals.";
            }

            if (prefs.Servers[0].BackupIntervalShort > prefs.Servers[0].BackupIntervalLong)
            {
                return "The short backup interval must be shorter than the long backup interval.";
            }

            return null;
        }
    }
}
