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

            this.WindowsStartField.Value = prefs.StartWithWindows;
            this.ServerStartField.Value = prefs.StartServerAutomatically;
            this.StartMinimizedField.Value = prefs.StartMinimized;
            this.CheckForUpdatesField.Value = prefs.CheckForUpdates.GetValueOrDefault(UserPreferences.Default.CheckForUpdates.Value);

            var startupInterval = TimeSpan.Parse(Resources.UpdateCheckInterval);
            this.CheckForUpdatesField.HelpText = this.CheckForUpdatesField.HelpText?.Replace("{startupInterval}", $"{startupInterval.TotalHours} hours");
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            var prefs = this.UserPrefsProvider.LoadPreferences();

            prefs.StartWithWindows = this.WindowsStartField.Value;
            prefs.StartServerAutomatically = this.ServerStartField.Value;
            prefs.StartMinimized = this.StartMinimizedField.Value;
            prefs.CheckForUpdates = this.CheckForUpdatesField.Value;

            StartupHelper.ApplyStartupSetting(prefs.StartWithWindows, this.Logger);

            this.UserPrefsProvider.SavePreferences(prefs);
            this.Close();
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ButtonDefaults_Click(object sender, EventArgs e)
        {
            this.WindowsStartField.Value = UserPreferences.Default.StartWithWindows;
            this.ServerStartField.Value = UserPreferences.Default.StartServerAutomatically;
            this.StartMinimizedField.Value = UserPreferences.Default.StartMinimized;
            this.CheckForUpdatesField.Value = UserPreferences.Default.CheckForUpdates.Value;
        }
    }
}
