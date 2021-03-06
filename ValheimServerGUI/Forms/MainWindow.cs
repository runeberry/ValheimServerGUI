using System;
using System.Linq;
using System.Windows.Forms;
using ValheimServerGUI.Game;
using ValheimServerGUI.Properties;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Preferences;

namespace ValheimServerGUI.Forms
{
    public partial class MainWindow : Form
    {
        private readonly IFormProvider FormProvider;
        private readonly IUserPreferences UserPrefs;
        private readonly ValheimServer Server;

        private bool IsUserClosing;

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
            this.Server.FilteredLogger.LogReceived += this.OnLogReceived;
            this.Server.StatusChanged += this.OnServerStatusChanged;
        }

        private void InitializeFormEvents()
        {
            this.MenuItemFileDirectories.Click += this.MenuItemFileDirectories_Clicked;
            this.MenuItemFileClose.Click += this.MenuItemFileClose_Clicked;

            this.MenuItemHelpUpdates.Click += this.MenuItemHelpUpdates_Clicked;
            this.MenuItemHelpAbout.Click += this.MenuItemHelpAbout_Clicked;

            this.ShowPasswordField.ValueChanged += this.ShowPasswordField_Changed;
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
            this.ServerPortField.Value = UserPrefs.GetNumberValue(PrefKeys.ServerPort, int.Parse(Resources.DefaultServerPort));
            this.ServerPasswordField.Value = UserPrefs.GetValue(PrefKeys.ServerPassword);
            this.CommunityServerField.Value = UserPrefs.GetFlagValue(PrefKeys.ServerPublic);
            this.ShowPasswordField.Value = false;

            SetFormStateForServer();
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

            this.SetFormStateForServer();
        }

        #endregion

        #region Form Event Handlers

        private void ButtonStartServer_Click(object sender, EventArgs e)
        {
            var options = new ValheimServerOptions
            {
                ExePath = UserPrefs.GetEnvironmentValue(PrefKeys.ValheimServerPath),
                Name = this.ServerNameField.Value,
                Password = this.ServerPasswordField.Value,
                WorldName = this.WorldSelectField.Value,
                Public = this.CommunityServerField.Value,
                Port = this.ServerPortField.Value,
            };

            try
            {
                options.Validate();
                Server.Start(options);
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

            // User preferences are saved each time the server is started
            UserPrefs.SetValue(PrefKeys.ServerName, this.ServerNameField.Value);
            UserPrefs.SetValue(PrefKeys.ServerPort, this.ServerPortField.Value);
            UserPrefs.SetValue(PrefKeys.ServerPassword, this.ServerPasswordField.Value);
            UserPrefs.SetValue(PrefKeys.ServerWorldName, this.WorldSelectField.Value);
            UserPrefs.SetValue(PrefKeys.ServerPublic, this.CommunityServerField.Value);
            
            UserPrefs.SaveFile();
        }

        private void ButtonStopServer_Click(object sender, EventArgs e)
        {
            Server.Stop();
        }

        private void ButtonRestartServer_Click(object sender, EventArgs e)
        {
            Server.Restart();
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
            base.OnFormClosing(e);

            if (e.CloseReason == CloseReason.UserClosing)
            {
                if (this.Server.IsAnyStatus(ServerStatus.Starting, ServerStatus.Running))
                {
                    // Server is still running, prompt the user to confirm they want to stop it

                    var result = MessageBox.Show(
                        "The Valheim server is still running. Do you want to stop the server " +
                        "and close this application?",
                        "Warning",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.Yes)
                    {
                        this.Server.Stop();
                        this.CloseApplicationOnServerStopped();
                    }

                    // Cancel the form close regardless of the user's choice
                    e.Cancel = true;
                }
                else if (this.Server.IsAnyStatus(ServerStatus.Stopping))
                {
                    var result = MessageBox.Show(
                        "The Valheim server is currently shutting down. Close anyway?" + Environment.NewLine +
                        "This could result in a loss of save data!",
                        "Warning",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    
                    if (result == DialogResult.No)
                    {
                        // Cancel the form close event, keep running the app
                        e.Cancel = true;
                        return;
                    }

                    // Otherwise, close out this window anyway
                }

                // Otherwise, the server is stopped, so we can safely close out this window
            }
            else if (e.CloseReason == CloseReason.WindowsShutDown)
            {
                // Try to stop the server, and keep the app running until we're sure it's stopped
                this.Server.Stop();
                this.CloseApplicationOnServerStopped();
                e.Cancel = true;
            }
            else
            {
                // Try to stop the server, but don't wait around for the results
                this.Server.Stop();
            }
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
            this.Close();
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

        private void SetFormStateForServer()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(SetFormStateForServer), new object[] { });
                return;
            }

            // Only allow form field changes when the server is stopped
            bool allowServerChanges = this.Server.Status == ServerStatus.Stopped;

            this.ServerNameField.Enabled = allowServerChanges;
            this.ServerPortField.Enabled = allowServerChanges;
            this.ServerPasswordField.Enabled = allowServerChanges;
            this.WorldSelectField.Enabled = allowServerChanges;
            this.CommunityServerField.Enabled = allowServerChanges;

            // You can only start the server when it's stopped
            this.ButtonStartServer.Enabled = this.Server.CanStart;
            this.ButtonStopServer.Enabled = this.Server.CanStop;
            this.ButtonRestartServer.Enabled = this.Server.CanRestart;
        }

        private void CloseApplicationOnServerStopped()
        {
            if (this.Server.IsAnyStatus(ServerStatus.Stopped))
            {
                Application.Exit();
                return;
            }

            this.Server.StatusChanged += (_, status) =>
            {
                if (status == ServerStatus.Stopped)
                {
                    Application.Exit();
                }
            };
        }

        #endregion
    }
}
