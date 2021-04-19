using Microsoft.Extensions.Logging;
using System;
using System.Windows.Forms;
using ValheimServerGUI.Game;
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
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            var prefs = this.UserPrefsProvider.LoadPreferences();

            prefs.StartWithWindows = this.WindowsStartField.Value;
            prefs.StartServerAutomatically = this.ServerStartField.Value;

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
        }
    }
}
