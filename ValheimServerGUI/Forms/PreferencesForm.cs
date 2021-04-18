using System;
using System.Windows.Forms;
using ValheimServerGUI.Game;

namespace ValheimServerGUI.Forms
{
    public partial class PreferencesForm : Form
    {
        private readonly IUserPreferencesProvider UserPrefsProvider;

        public PreferencesForm()
        {
            InitializeComponent();
        }

        public PreferencesForm(IUserPreferencesProvider userPrefsProvider) : this()
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

            this.WindowsStartField.Value = prefs.StartWithWindows;
            this.ServerStartField.Value = prefs.StartServerAutomatically;
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            var prefs = this.UserPrefsProvider.LoadPreferences();

            prefs.StartWithWindows = this.WindowsStartField.Value;
            prefs.StartServerAutomatically = this.ServerStartField.Value;

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
