using Microsoft.Extensions.Logging;
using System;
using System.Windows.Forms;
using ValheimServerGUI.Game;
using ValheimServerGUI.Properties;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.Forms
{
    public partial class PreferencesForm : Form
    {
        private readonly IUserPreferencesProvider UserPrefsProvider;
        private readonly ILogger Logger;

        public PreferencesForm()
        {
            InitializeComponent();
            this.AddApplicationIcon();
        }

        public PreferencesForm(IUserPreferencesProvider userPrefsProvider, ILogger logger) : this()
        {
            this.UserPrefsProvider = userPrefsProvider;
            this.Logger = logger;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            InitializeFormFields();
        }

        private void InitializeFormFields()
        {
            var prefs = this.UserPrefsProvider.LoadPreferences();

            this.SaveProfileOnStartField.Value = prefs.SaveProfileOnStart;
            this.CheckForUpdatesField.Value = prefs.CheckForUpdates;

            var startupInterval = TimeSpan.Parse(Resources.UpdateCheckInterval);
            this.CheckForUpdatesField.HelpText = this.CheckForUpdatesField.HelpText?.Replace("{startupInterval}", $"{startupInterval.TotalHours} hours");
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            var prefs = this.UserPrefsProvider.LoadPreferences();

            prefs.SaveProfileOnStart = this.SaveProfileOnStartField.Value;
            prefs.CheckForUpdates = this.CheckForUpdatesField.Value;

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

            this.SaveProfileOnStartField.Value = prefs.SaveProfileOnStart;
            this.CheckForUpdatesField.Value = prefs.CheckForUpdates;
        }
    }
}
