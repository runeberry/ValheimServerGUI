using System;
using System.Windows.Forms;
using ValheimServerGUI.Game;
using ValheimServerGUI.Properties;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Logging;

namespace ValheimServerGUI.Forms
{
    public partial class PreferencesForm : Form
    {
        private readonly IUserPreferencesProvider UserPrefsProvider;
        private readonly IApplicationLogger Logger;

        public PreferencesForm()
        {
            InitializeComponent();
            this.AddApplicationIcon();
        }

        public PreferencesForm(IUserPreferencesProvider userPrefsProvider, IApplicationLogger logger) : this()
        {
            UserPrefsProvider = userPrefsProvider;
            Logger = logger;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            InitializeFormFields();
        }

        private void InitializeFormFields()
        {
            var prefs = UserPrefsProvider.LoadPreferences();

            SaveProfileOnStartField.Value = prefs.SaveProfileOnStart;
            CheckForUpdatesField.Value = prefs.CheckForUpdates;
            StartWithWindowsField.Value = prefs.StartWithWindows;
            StartMinimizedField.Value = prefs.StartMinimized;
            WriteLogFileField.Value = prefs.WriteApplicationLogsToFile;
            PasswordValidationField.Value = prefs.EnablePasswordValidation;

            var startupInterval = TimeSpan.Parse(Resources.UpdateCheckInterval);
            CheckForUpdatesField.HelpText = CheckForUpdatesField.HelpText?.Replace("{startupInterval}", $"{startupInterval.TotalHours} hours");
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            var prefs = UserPrefsProvider.LoadPreferences();

            prefs.SaveProfileOnStart = SaveProfileOnStartField.Value;
            prefs.CheckForUpdates = CheckForUpdatesField.Value;
            prefs.StartWithWindows = StartWithWindowsField.Value;
            prefs.StartMinimized = StartMinimizedField.Value;
            prefs.WriteApplicationLogsToFile = WriteLogFileField.Value;
            prefs.EnablePasswordValidation = PasswordValidationField.Value;

            StartupHelper.ApplyStartupSetting(prefs.StartWithWindows, Logger);

            UserPrefsProvider.SavePreferences(prefs);
            Close();
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ButtonDefaults_Click(object sender, EventArgs e)
        {
            var prefs = UserPreferences.GetDefault();

            SaveProfileOnStartField.Value = prefs.SaveProfileOnStart;
            CheckForUpdatesField.Value = prefs.CheckForUpdates;
            StartWithWindowsField.Value = prefs.StartWithWindows;
            StartMinimizedField.Value = prefs.StartMinimized;
            WriteLogFileField.Value = prefs.WriteApplicationLogsToFile;
            PasswordValidationField.Value = prefs.EnablePasswordValidation;
        }
    }
}
