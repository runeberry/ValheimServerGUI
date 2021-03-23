using System;
using System.Collections.Generic;
using System.Drawing;
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
        private static readonly string NL = Environment.NewLine;
        private const string PlayerNameUnknown = "(unknown)";

        private readonly IFormProvider FormProvider;
        private readonly IUserPreferences UserPrefs;
        private readonly IValheimFileProvider FileProvider;
        private readonly IPlayerDataProvider PlayerDataProvider;
        private readonly ValheimServer Server;

        public MainWindow(
            IFormProvider formProvider,
            IUserPreferences userPrefs,
            IValheimFileProvider fileProvider,
            IPlayerDataProvider playerDataProvider,
            ValheimServer server)
        {
            this.FormProvider = formProvider;
            this.UserPrefs = userPrefs;
            this.FileProvider = fileProvider;
            this.PlayerDataProvider = playerDataProvider;
            this.Server = server;

            InitializeComponent(); // WinForms generated code, always first
            InitializeImages();
            InitializeServer();
            InitializeFormEvents();
            InitializeFormFields(); // Display data back to user, always last
        }

        #region Initialization

        private void InitializeImages()
        {
            this.ImageList.AddImagesFromResourceFile(typeof(Resources));
        }

        private void InitializeServer()
        {
            this.Server.FilteredLogger.LogReceived += this.OnLogReceived;
            this.Server.StatusChanged += this.OnServerStatusChanged;
            this.Server.WorldSaved += this.OnWorldSaved;

            this.PlayerDataProvider.PlayerStatusChanged += this.OnPlayerStatusChanged;
        }

        private void InitializeFormEvents()
        {
            // Menu items
            this.MenuItemFileDirectories.Click += this.MenuItemFileDirectories_Clicked;
            this.MenuItemFileClose.Click += this.MenuItemFileClose_Clicked;
            this.MenuItemHelpManual.Click += this.MenuItemHelpManual_Click;
            this.MenuItemHelpUpdates.Click += this.MenuItemHelpUpdates_Clicked;
            this.MenuItemHelpAbout.Click += this.MenuItemHelpAbout_Clicked;

            // Tray icon
            this.NotifyIcon.MouseClick += this.NotifyIcon_MouseClick;
            this.TrayContextMenuStart.Click += this.ButtonStartServer_Click;
            this.TrayContextMenuRestart.Click += this.ButtonRestartServer_Click;
            this.TrayContextMenuStop.Click += this.ButtonStopServer_Click;
            this.TrayContextMenuClose.Click += this.MenuItemFileClose_Clicked;

            // Timers
            this.ServerRefreshTimer.Tick += this.ServerRefreshTimer_Tick;

            // Tabs
            this.TabPlayers.VisibleChanged += this.TabPlayers_VisibleChanged;

            // Buttons
            this.ButtonStartServer.Click += this.ButtonStartServer_Click;
            this.ButtonRestartServer.Click += this.ButtonRestartServer_Click;
            this.ButtonStopServer.Click += this.ButtonStopServer_Click;
            this.ButtonClearLogs.Click += this.ButtonClearLogs_Click;

            // Form fields
            this.ShowPasswordField.ValueChanged += this.ShowPasswordField_Changed;
            this.WorldSelectRadioExisting.ValueChanged += this.WorldSelectRadioExisting_Changed;
            this.WorldSelectRadioNew.ValueChanged += this.WorldSelectRadioNew_Changed;
        }

        private void InitializeFormFields()
        {
            this.PlayersTable.AddRowBinding<PlayerInfo>(row =>
            {
                row.AddCellBinding(this.ColumnPlayerName.Index, p => p.PlayerName ?? PlayerNameUnknown);
                row.AddCellBinding(this.ColumnPlayerStatus.Index, p => p.PlayerStatus);
                row.AddCellBinding(this.ColumnPlayerUpdated.Index, p => new TimeAgo(p.LastStatusChange));
            });

            this.RefreshFormFields();
            this.LoadFormValuesFromUserPrefs();
        }

        #endregion

        #region MainWindow Events

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            this.CheckFilePaths();

            this.SetStatusText("Loaded OK");
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (this.WindowState == FormWindowState.Minimized)
            {
                this.ShowInTaskbar = false;
            }
            else
            {
                this.ShowInTaskbar = true;
            }
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
            this.ShowDirectoriesForm();
        }

        private void MenuItemFileClose_Clicked(object sender, EventArgs e)
        {
            this.Close();
        }

        private void MenuItemHelpManual_Click(object sender, EventArgs e)
        {
            WebHelper.OpenWebAddress(Resources.UrlHelp);
        }

        private void MenuItemHelpUpdates_Clicked(object sender, EventArgs e)
        {
            WebHelper.OpenWebAddress(Resources.UrlUpdates);
        }

        private void MenuItemHelpAbout_Clicked(object sender, EventArgs e)
        {
            var aboutForm = FormProvider.GetForm<AboutForm>();
            aboutForm.ShowDialog();
        }

        #endregion

        #region Form Field Events

        private void ButtonStartServer_Click(object sender, EventArgs e)
        {
            string worldName;

            if (this.WorldSelectRadioNew.Value)
            {
                // Creating a new world, ensure that the name is available
                worldName = this.WorldSelectNewNameField.Value;
                if (!this.FileProvider.IsWorldNameAvailable(worldName))
                {
                    MessageBox.Show(
                        $"A world named '{worldName}' already exists.",
                        "Server Configuration Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    this.WorldSelectRadioExisting.Value = true;
                    return;
                }
            }
            else
            {
                // Using an existing world, ensure that the file exists
                worldName = this.WorldSelectExistingNameField.Value;
                if (this.FileProvider.IsWorldNameAvailable(worldName))
                {
                    // Don't think this is possible to hit because the name comes from a dropdown
                    MessageBox.Show(
                        $"No world exists with name '{worldName}'.",
                        "Server Configuration Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
            }

            var options = new ValheimServerOptions
            {
                Name = this.ServerNameField.Value,
                Password = this.ServerPasswordField.Value,
                WorldName = worldName, // Server automatically creates a new world if a world doesn't yet exist w/ that name
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
            UserPrefs.SetValue(PrefKeys.ServerWorldName, worldName);
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

        private void NotifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (this.WindowState == FormWindowState.Minimized)
                {
                    this.WindowState = FormWindowState.Normal;
                }
            }
        }

        private void WorldSelectRadioExisting_Changed(object sender, bool value)
        {
            this.WorldSelectExistingNameField.Visible = value;

            if (value)
            {
                // When going back to the Existing World field, select the New Name if it's already an existing world
                this.WorldSelectExistingNameField.Value = this.WorldSelectNewNameField.Value;
                
                // Then, empty out the new name field
                this.WorldSelectNewNameField.Value = null;
            }
        }

        private void WorldSelectRadioNew_Changed(object sender, bool value)
        {
            this.WorldSelectNewNameField.Visible = value;
        }

        private void TabPlayers_VisibleChanged(object sender, EventArgs e)
        {
            if (this.TabPlayers.Visible)
            {
                this.UpdatePlayerStatus();
                this.ServerRefreshTimer.Enabled = true;
            }
            else
            {
                this.ServerRefreshTimer.Enabled = false;
            }
        }

        private void ServerRefreshTimer_Tick(object sender, EventArgs e)
        {
            this.UpdatePlayerStatus();
        }

        #endregion

        #region Server Events

        private void OnLogReceived(object sender, LogEvent logEvent)
        {
            // This technique allows cross-thread access to UI controls
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
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<object, ServerStatus>(OnServerStatusChanged), new object[] { sender, status });
                return;
            }

            this.SetStatusText(status.ToString());

            this.RefreshFormStateForServer();

            if (status == ServerStatus.Running && this.WorldSelectRadioNew.Value)
            {
                // Once a "new world" starts running, switch back to the Existing Worlds screen
                // and select the newly created world
                this.RefreshWorldSelect();
                this.WorldSelectRadioExisting.Value = true;
            }
        }

        private void OnPlayerStatusChanged(object sender, PlayerInfo player)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<object, PlayerInfo>(OnPlayerStatusChanged), new object[] { sender, player });
                return;
            }

            this.UpdatePlayerStatus(player);
        }

        private void OnWorldSaved(object sender, decimal duration)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<object, decimal>(OnWorldSaved), new object[] { sender, duration });
                return;
            }

            // todo: something
        }

        #endregion

        #region Common Methods

        private void CheckFilePaths()
        {
            try
            {
                var _ = this.FileProvider.ServerExe;
            }
            catch (Exception e)
            {
                var result = MessageBox.Show(
                    $"{e.Message}{NL}{NL}" +
                    $"This may occur if you do not have Valheim Dedicated Server installed, or if you have " +
                    $"installed it in a different directory. See Help for more info.{NL}{NL}" +
                    "Would you like to change your directories now?",
                    "File Not Found",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    this.ShowDirectoriesForm();
                }
            }
        }

        private void ShowDirectoriesForm()
        {
            this.FormProvider.GetForm<DirectoriesForm>().ShowDialog();
            this.RefreshFormFields();
        }

        private void AddLog(string message)
        {
            this.TextBoxLogs.AppendLine(message);
        }

        private void ClearLogs()
        {
            this.TextBoxLogs.Text = "";
        }

        private void SetStatusText(string message)
        {
            this.StatusStripLabel.Text = message;
        }

        private int GetImageIndex(string key)
        {
            return this.ImageList.Images.IndexOfKey(key);
        }

        private void UpdatePlayerStatus()
        {
            foreach (var player in this.PlayerDataProvider.Data)
            {
                this.UpdatePlayerStatus(player);
            }
        }

        private void UpdatePlayerStatus(PlayerInfo player)
        {
            var playerName = player.PlayerName ?? PlayerNameUnknown;

            var playerRow = this.PlayersTable
                .GetRowsWithType<PlayerInfo>()
                .FirstOrDefault(p => p.Entity.SteamId == player.SteamId);

            if (playerRow == null)
            {
                playerRow = this.PlayersTable.AddRowFromEntity(player);
            }
            else
            {
                playerRow.RefreshValues();
            }

            // Update icon based on player status
            var imageIndex = -1;
            switch (player.PlayerStatus)
            {
                case PlayerStatus.Online:
                    imageIndex = this.GetImageIndex(nameof(Resources.StatusOK_16x));
                    break;
                case PlayerStatus.Joining:
                case PlayerStatus.Leaving:
                    imageIndex = this.GetImageIndex(nameof(Resources.UnsyncedCommits_16x_Horiz));
                    break;
                case PlayerStatus.Offline:
                    imageIndex = this.GetImageIndex(nameof(Resources.StatusNotStarted_16x));
                    break;
            }
            playerRow.ImageIndex = imageIndex;

            // Update font based on player status
            var color = this.PlayersTable.ForeColor;
            switch (player.PlayerStatus)
            {
                case PlayerStatus.Offline:
                    color = Color.Gray;
                    break;
            }
            playerRow.ForeColor = color;
        }

        private void RefreshFormFields()
        {
            this.RefreshWorldSelect();
            this.RefreshFormStateForServer();
        }

        private void RefreshFormStateForServer()
        {
            // Only allow form field changes when the server is stopped
            bool allowServerChanges = this.Server.Status == ServerStatus.Stopped;

            this.ServerNameField.Enabled = allowServerChanges;
            this.ServerPortField.Enabled = allowServerChanges;
            this.ServerPasswordField.Enabled = allowServerChanges;
            this.WorldSelectGroupBox.Enabled = allowServerChanges;
            this.CommunityServerField.Enabled = allowServerChanges;

            this.ButtonStartServer.Enabled = this.Server.CanStart;
            this.ButtonRestartServer.Enabled = this.Server.CanRestart;
            this.ButtonStopServer.Enabled = this.Server.CanStop;

            // Tray items are enabled based on their button equivalents
            this.TrayContextMenuStart.Enabled = this.ButtonStartServer.Enabled;
            this.TrayContextMenuRestart.Enabled = this.ButtonRestartServer.Enabled;
            this.TrayContextMenuStop.Enabled = this.ButtonStopServer.Enabled;
        }

        private void RefreshWorldSelect()
        {
            try
            {
                // Refresh the existing worlds list
                var worlds = this.FileProvider.GetWorldNames();
                this.WorldSelectExistingNameField.DataSource = worlds;
                this.WorldSelectExistingNameField.DropdownEnabled = worlds.Any();
            }
            catch
            {
                // Show no worlds if something goes wrong
                this.WorldSelectExistingNameField.DataSource = null;
                this.WorldSelectExistingNameField.DropdownEnabled = false;
            }
        }

        private void LoadFormValuesFromUserPrefs()
        {
            this.ServerNameField.Value = UserPrefs.GetValue(PrefKeys.ServerName);
            this.ServerPortField.Value = UserPrefs.GetNumberValue(PrefKeys.ServerPort, int.Parse(Resources.DefaultServerPort));
            this.ServerPasswordField.Value = UserPrefs.GetValue(PrefKeys.ServerPassword);
            this.CommunityServerField.Value = UserPrefs.GetFlagValue(PrefKeys.ServerPublic);
            this.ShowPasswordField.Value = false;

            this.WorldSelectExistingNameField.Value = UserPrefs.GetValue(PrefKeys.ServerWorldName);
            this.WorldSelectRadioExisting.Value = true;
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
