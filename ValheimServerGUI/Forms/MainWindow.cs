using System;
using System.Linq;
using System.Windows.Forms;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Preferences;

namespace ValheimServerGUI.Forms
{
    public partial class MainWindow : Form
    {
        private readonly IFormProvider FormProvider;
        private readonly IUserPreferences UserPrefs;
        private readonly ValheimServer Server;

        public MainWindow(
            IFormProvider formProvider,
            IUserPreferences userPrefs, 
            ValheimServer server)
        {
            this.FormProvider = formProvider;
            this.UserPrefs = userPrefs;
            this.Server = server;

            InitializeComponent(); // WinForms generated code, always first

            InitializeServer();

            InitializeFormEvents();
            InitializeFormFields(); // Display data back to user, always last

            this.SetStatusText("Loaded OK");
        }

        #region Initialization

        private void InitializeServer()
        {
            this.Server.FilteredLogger.LogReceived += new EventHandler<LogEvent>(this.OnLogReceived);
            this.Server.StatusChanged += new EventHandler<ServerStatus>(this.OnServerStatusChanged);
        }

        private void InitializeFormEvents()
        {
            this.MenuItemFileDirectories.Click += new EventHandler(this.MenuItemFileDirectories_Clicked);
            this.MenuItemFileClose.Click += new EventHandler(this.MenuItemFileClose_Clicked);

            this.MenuItemHelpUpdates.Click += new EventHandler(this.MenuItemHelpUpdates_Clicked);
            this.MenuItemHelpAbout.Click += new EventHandler(this.MenuItemHelpAbout_Clicked);

            this.ShowPasswordField.ValueChanged += new EventHandler<bool>(this.ShowPasswordField_Changed);
        }

        private void InitializeFormFields()
        {
            try
            {
                var worlds = ValheimData.GetWorldNames(UserPrefs.GetEnvironmentValue(PrefKeys.ValheimWorldsFolder));
                this.WorldSelectField.DataSource = worlds;
                this.WorldSelectField.DropdownEnabled = worlds.Any();
                this.WorldSelectField.Value = UserPrefs.GetValue(PrefKeys.ServerWorldName);
            }
            catch (System.IO.DirectoryNotFoundException)
            {
                this.WorldSelectField.DataSource = null;
                this.WorldSelectField.DropdownEnabled = false;
            }

            this.ServerNameField.Value = UserPrefs.GetValue(PrefKeys.ServerName);
            this.ServerPasswordField.Value = UserPrefs.GetValue(PrefKeys.ServerPassword);
            this.CommunityServerField.Value = UserPrefs.GetFlagValue(PrefKeys.ServerPublic);
            this.ShowPasswordField.Value = false;
        }

        private void OnLogReceived(object sender, LogEvent logEvent)
        {
            // Allows cross-thread access to the TextBox
            // See here: https://stackoverflow.com/questions/519233/writing-to-a-textbox-from-another-thread
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<object, LogEvent>(OnLogReceived), new object[] { sender, logEvent });
                return;
            }

            this.AddLog(logEvent.Message);
        }

        private void OnServerStatusChanged(object sender, ServerStatus status)
        {
            this.SetStatusText(status.ToString());
        }

        #endregion

        #region Form Event Handlers

        private void ButtonStartServer_Click(object sender, EventArgs e)
        {
            Server.ServerPath = UserPrefs.GetEnvironmentValue(PrefKeys.ValheimServerPath);
            Server.ServerName = this.ServerNameField.Value;
            Server.ServerPassword = this.ServerPasswordField.Value;
            Server.WorldName = this.WorldSelectField.Value;
            Server.Public = this.CommunityServerField.Value;

            try
            {
                Server.Validate();
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    exception.Message,
                    "Server Configuration Error",
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);

                return;
            }

            Server.Start();

            UserPrefs.SetValue(PrefKeys.ServerName, this.ServerNameField.Value);
            UserPrefs.SetValue(PrefKeys.ServerPassword, this.ServerPasswordField.Value);
            UserPrefs.SetValue(PrefKeys.ServerWorldName, this.WorldSelectField.Value);
            UserPrefs.SetValue(PrefKeys.ServerPublic, this.CommunityServerField.Value);
            UserPrefs.SaveFile();
        }

        private void ButtonStopServer_Click(object sender, EventArgs e)
        {
            Server.Stop();
        }

        private void ButtonClearLogs_Click(object sender, EventArgs e)
        {
            this.ClearLogs();
        }

        private void ShowPasswordField_Changed(object sender, bool value)
        {
            this.ServerPasswordField.HideValue = !value;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            this.Server.ForceStop();

            base.OnFormClosing(e);
        }

        #endregion

        #region Menu Items

        private void MenuItemFileDirectories_Clicked(object sender, EventArgs e)
        {
            var directoriesForm = FormProvider.GetForm<DirectoriesForm>();
            directoriesForm.ShowDialog();

            InitializeFormFields();
        }

        private void MenuItemFileClose_Clicked(object sender, EventArgs e)
        {
            this.CloseApplication();
        }

        private void MenuItemHelpUpdates_Clicked(object sender, EventArgs e)
        {
            this.SetStatusText("Clicked Updates!");
        }

        private void MenuItemHelpAbout_Clicked(object sender, EventArgs e)
        {
            var aboutForm = FormProvider.GetForm<AboutForm>();
            aboutForm.ShowDialog();
        }

        #endregion

        #region Common Methods

        private void AddLog(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(AddLog), new object[] { message });
                return;
            }

            this.TextBoxLogs.AppendLine(message);
        }

        private void ClearLogs()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(ClearLogs));
                return;
            }

            this.TextBoxLogs.Text = "";
        }

        private void SetStatusText(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(SetStatusText), new object[] { message });
                return;
            }

            this.StatusStripLabel.Text = message;
        }

        private void CloseApplication()
        {
            Server.Stop();
            Application.Exit();
        }

        #endregion
    }
}
