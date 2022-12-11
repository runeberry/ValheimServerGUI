using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using ValheimServerGUI.Game;
using ValheimServerGUI.Properties;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Logging;
using ValheimServerGUI.Tools.Processes;

namespace ValheimServerGUI.Forms
{
    public partial class MainWindow : Form
    {
#if DEBUG
        private static readonly bool SimulateConstructorException = false;
        private static readonly bool SimulateStartServerException = false;
        private static readonly bool SimulateStopServerException = false;
#endif
        private string _currentProfile;
        public string CurrentProfile
        {
            get => this._currentProfile;
            set
            {
                this._currentProfile = value;
                this.ProfileChanged?.Invoke(this, this._currentProfile);
            }
        }
        public EventHandler<string> ProfileChanged;
        public int SplashIndex { get; set; }

        /// <summary>
        /// Load this profile into the form when the window is first loaded.
        /// </summary>
        public string StartProfile { get; set; }

        /// <summary>
        /// If true, start the server from the StartProfile settings as soon
        /// as the window is loaded.
        /// </summary>
        public bool StartServerAutomatically { get; set; }

        private static readonly string NL = Environment.NewLine;
        private const string LogViewServer = "Server";
        private const string LogViewApplication = "Application";
        private const string IpLoadingText = "Loading...";

        private readonly Stopwatch ServerUptimeTimer = new();
        private readonly Queue<decimal> WorldSaveTimes = new();
        private readonly Dictionary<ServerStatus, Image> ServerStatusIconMap = new()
        {
            { ServerStatus.Stopped, Resources.StatusPause_grey_16x },
            { ServerStatus.Starting, Resources.UnsyncedCommits_16x_Horiz },
            { ServerStatus.Running, Resources.StatusRun_16x },
            { ServerStatus.Stopping, Resources.UnsyncedCommits_16x_Horiz },
        };

        private readonly IFormProvider FormProvider;
        private readonly IUserPreferencesProvider UserPrefsProvider;
        private readonly IServerPreferencesProvider ServerPrefsProvider;
        private readonly IValheimFileProvider FileProvider;
        private readonly IPlayerDataRepository PlayerDataProvider;
        private readonly ValheimServer Server;
        private readonly IEventLogger Logger;
        private readonly IIpAddressProvider IpAddressProvider;
        private readonly ISoftwareUpdateProvider SoftwareUpdateProvider;
        private readonly IProcessProvider ProcessProvider;
        private readonly IStartupArgsProvider StartupArgsProvider;

        public MainWindow(
            IFormProvider formProvider,
            IUserPreferencesProvider userPrefsProvider,
            IServerPreferencesProvider serverPrefsProvider,
            IValheimFileProvider fileProvider,
            IPlayerDataRepository playerDataProvider,
            ValheimServer server,
            IEventLogger appLogger,
            IIpAddressProvider ipAddressProvider,
            ISoftwareUpdateProvider softwareUpdateProvider,
            IProcessProvider processProvider,
            IStartupArgsProvider startupArgsProvider)
        {
#if DEBUG
            if (SimulateConstructorException) throw new InvalidOperationException("Intentional exception thrown for testing");
#endif
            this.FormProvider = formProvider;
            this.UserPrefsProvider = userPrefsProvider;
            this.ServerPrefsProvider = serverPrefsProvider;
            this.FileProvider = fileProvider;
            this.PlayerDataProvider = playerDataProvider;
            this.Server = server;
            this.Logger = appLogger;
            this.IpAddressProvider = ipAddressProvider;
            this.SoftwareUpdateProvider = softwareUpdateProvider;
            this.ProcessProvider = processProvider;
            this.StartupArgsProvider = startupArgsProvider;

            InitializeComponent(); // WinForms generated code, always first
            this.AddApplicationIcon();
            InitializeImages();
            InitializeServices();
            InitializeFormEvents();
            InitializeFormFields(); // Display data back to user, always last
        }

        #region Initialization

        private void InitializeImages()
        {
            this.ImageList.AddImagesFromResourceFile(typeof(Resources));
        }

        private void InitializeServices()
        {
            this.Logger.LogReceived += this.BuildEventHandler<EventLogContext>(this.OnApplicationLogReceived);
            this.Server.LogReceived += this.BuildEventHandler<EventLogContext>(this.OnServerLogReceived);
            this.Server.StatusChanged += this.BuildEventHandler<ServerStatus>(this.OnServerStatusChanged);
            this.Server.WorldSaved += this.BuildEventHandler<decimal>(this.OnWorldSaved);
            this.Server.InviteCodeReady += this.BuildEventHandler<string>(this.OnInviteCodeReady);

            this.PlayerDataProvider.EntityUpdated += this.BuildEventHandler<PlayerInfo>(this.OnPlayerUpdated);

            this.IpAddressProvider.ExternalIpChanged += this.BuildEventHandler<string>(this.IpAddressProvider_ExternalIpChanged);
            this.IpAddressProvider.InternalIpChanged += this.BuildEventHandler<string>(this.IpAddressProvider_InternalIpChanged);

            this.SoftwareUpdateProvider.UpdateCheckStarted += this.BuildEventHandler(this.SoftwareUpdateProvider_UpdateCheckStarted);
            this.SoftwareUpdateProvider.UpdateCheckFinished += this.BuildEventHandler<SoftwareUpdateEventArgs>(this.SoftwareUpdateProvider_UpdateCheckFinished);

            this.ServerPrefsProvider.PreferencesSaved += this.BuildEventHandler<List<ServerPreferences>>(this.OnPreferencesSaved);
        }

        private void InitializeFormEvents()
        {
            // MainWindow
            this.ProfileChanged += this.BuildEventHandler<string>(this.OnProfileChanged);

            // Menu items
            this.MenuItemFileNewWindow.Click += this.MenuItemFileNewWindow_Click;
            this.MenuItemFileNewProfile.Click += this.MenuItemFileNewProfile_Click;
            this.MenuItemFileSaveProfile.Click += this.MenuItemFileSaveProfile_Click;
            this.MenuItemFileSaveProfileAs.Click += this.MenuItemFileSaveProfileAs_Click;
            this.MenuItemFilePreferences.Click += this.MenuItemFilePreferences_Click;
            this.MenuItemFileDirectories.Click += this.MenuItemFileDirectories_Click;
            this.MenuItemFileOpenSettings.Click += this.MenuItemFileOpenSettings_Click;
            this.MenuItemFileClose.Click += this.MenuItemFileClose_Click;
            this.MenuItemHelpManual.Click += this.MenuItemHelpManual_Click;
            this.MenuItemHelpPortForwarding.Click += this.MenuItemHelpPortForwarding_Click;
            this.MenuItemHelpBugReport.Click += this.MenuItemHelpBugReport_Click;
            this.MenuItemHelpUpdates.Click += this.MenuItemHelpUpdates_Click;
            this.MenuItemHelpAbout.Click += this.MenuItemHelpAbout_Click;

            // Tray icon
            this.NotifyIcon.MouseClick += this.NotifyIcon_MouseClick;
            this.TrayContextMenuServerName.Click += this.TrayContextMenuServerName_Click;
            this.TrayContextMenuStart.Click += this.BuildEventHandler(this.ButtonStartServer_Click);
            this.TrayContextMenuRestart.Click += this.BuildEventHandler(this.ButtonRestartServer_Click);
            this.TrayContextMenuStop.Click += this.BuildEventHandler(this.ButtonStopServer_Click);
            this.TrayContextMenuClose.Click += this.MenuItemFileClose_Click;

            // Timers
            this.ServerRefreshTimer.Tick += this.ServerRefreshTimer_Tick;
            this.UpdateCheckTimer.Tick += this.BuildEventHandler(this.UpdateCheckTimer_Tick);

            // Tabs
            this.TabPlayers.VisibleChanged += this.TabPlayers_VisibleChanged;
            this.TabServerDetails.VisibleChanged += this.BuildEventHandler(this.TabServerDetails_VisibleChanged);

            // Buttons
            this.ButtonStartServer.Click += this.BuildEventHandler(this.ButtonStartServer_Click);
            this.ButtonRestartServer.Click += this.BuildEventHandler(this.ButtonRestartServer_Click);
            this.ButtonStopServer.Click += this.BuildEventHandler(this.ButtonStopServer_Click);
            this.ButtonClearLogs.Click += this.ButtonClearLogs_Click;
            this.ButtonSaveLogs.Click += this.ButtonSaveLogs_Click;
            this.ButtonPlayerDetails.Click += this.ButtonPlayerDetails_Click;
            this.ButtonRemovePlayer.Click += this.ButtonRemovePlayer_Click;
            this.CopyButtonServerPassword.CopyFunction = () => this.ServerPasswordField.Value;
            this.CopyButtonExternalIpAddress.CopyFunction = () => this.LabelExternalIpAddress.Value;
            this.CopyButtonInternalIpAddress.CopyFunction = () => this.LabelInternalIpAddress.Value;
            this.CopyButtonLocalIpAddress.CopyFunction = () => this.LabelLocalIpAddress.Value;
            this.CopyButtonInviteCode.CopyFunction = () => this.LabelInviteCode.Value;
            this.StatusStripLabelRight.Click += this.BuildEventHandler(this.StatusStripLabelRight_Click);

            // Form fields
            this.ServerNameField.ValueChanged += this.ServerNameField_Changed;
            this.ShowPasswordField.ValueChanged += this.ShowPasswordField_Changed;
            this.WorldSelectRadioExisting.ValueChanged += this.WorldSelectRadioExisting_Changed;
            this.WorldSelectRadioNew.ValueChanged += this.WorldSelectRadioNew_Changed;
            this.WorldSelectExistingNameField.EnabledChanged += this.WorldSelectExistingNameField_EnabledChanged;
            this.LogViewSelectField.ValueChanged += this.LogViewSelectField_Changed;
            this.PlayersTable.SelectionChanged += this.PlayersTable_SelectionChanged;
        }

        private void InitializeFormFields()
        {
            this.LogViewSelectField.DataSource = new[] { LogViewServer, LogViewApplication };
            this.LogViewSelectField.Value = LogViewServer;

            this.RefreshFormFields();
        }

        private void InitializeStartupPrefs()
        {
            // If StartProfile was set by SplashForm before this window was shown, then
            // load that server profile up now, and start the server is settings indicate that.
            if (this.StartProfile != null)
            {
                var serverPrefs = this.SetFormStateFromPrefs(this.StartProfile);
                if (serverPrefs != null && this.StartServerAutomatically)
                {
                    this.StartServer(false);
                }
                else
                {
                    this.OnServerStatusChanged(ServerStatus.Stopped);
                }
            }
            else
            {
                // No server to start, mock a "stopped" event to initialize the form
                this.OnServerStatusChanged(ServerStatus.Stopped);
            }
        }

        private void InitializeIpAddresses()
        {
            var internalIp = this.IpAddressProvider.InternalIpAddress;
            var externalIp = this.IpAddressProvider.ExternalIpAddress;

            this.LabelInternalIpAddress.Value = internalIp ?? IpLoadingText;
            this.LabelExternalIpAddress.Value = externalIp ?? IpLoadingText;

            if (internalIp == null) this.IpAddressProvider.LoadInternalIpAddressAsync();
            if (externalIp == null) this.IpAddressProvider.LoadExternalIpAddressAsync();

            this.RefreshIpPorts();
        }

        private void InitializePlayerData()
        {
            this.PlayersTable.AddRowBinding<PlayerInfo>(row =>
            {
                row.AddCellBinding(this.ColumnPlayerName.Index, p => p.PlayerName ?? $"(...{p.SteamId[^4..]})");
                row.AddCellBinding(this.ColumnPlayerStatus.Index, p => p.PlayerStatus);
                row.AddCellBinding(this.ColumnPlayerUpdated.Index, p => new TimeAgo(p.LastStatusChange));
            });

            foreach (var playerInfo in this.PlayerDataProvider.Data)
            {
                this.SetPlayerStatus(playerInfo);
            }
        }

        #endregion

        #region MainWindow Events

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            this.InitializeIpAddresses();
            this.InitializePlayerData();
            this.InitializeStartupPrefs();
            this.CheckFilePaths();
            this.NotifyIcon.Visible = true;

            this.Logger.LogInformation("Valheim Server GUI v{version} - Loaded OK", AssemblyHelper.GetApplicationVersion());
        }

        private void OnProfileChanged(string _)
        {
            this.RefreshCurrentProfileDisplayed();
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
                        "and close this window?",
                        "Warning",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.Yes)
                    {
                        this.Server.Stop();
                        this.CloseWindowOnServerStopped();
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
                this.CloseWindowOnServerStopped();
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

        private void MenuItemFileNewWindow_Click(object sender, EventArgs e)
        {
            this.LaunchNewWindow();
        }

        private void MenuItemFileNewProfile_Click(object sender, EventArgs e)
        {
            var profileName = this.PromptForProfileName();
            if (profileName == null) return;

            var prefs = new ServerPreferences { ProfileName = profileName, Name = profileName };
            this.ServerPrefsProvider.SavePreferences(prefs);

            this.SetFormStateFromPrefs(prefs);
        }

        private void MenuItemFileSaveProfile_Click(object sender, EventArgs e)
        {
            var prefs = this.GetPrefsFromFormState();
            this.ServerPrefsProvider.SavePreferences(prefs);
        }

        private void MenuItemFileSaveProfileAs_Click(object sender, EventArgs e)
        {
            var profileName = this.PromptForProfileName($"Copy of {this.CurrentProfile}");
            if (profileName == null) return;

            var prefs = this.GetPrefsFromFormState();
            prefs.ProfileName = profileName;
            this.ServerPrefsProvider.SavePreferences(prefs);
            this.CurrentProfile = profileName;
        }

        private void MenuItemFileLoadProfileItem_Click(object sender, EventArgs e)
        {
            if (sender is not ToolStripItem item) return;

            this.SetFormStateFromPrefs(item.Text);
        }

        private void MenuItemFileRemoveProfileItem_Click(object sender, EventArgs e)
        {
            if (sender is not ToolStripItem item) return;

            var profileName = item.Text;
            var result = MessageBox.Show(
                $"Remove server profile '{profileName}'?",
                "Remove Profile",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                this.ServerPrefsProvider.RemovePreferences(profileName);
            }
        }

        private void MenuItemFilePreferences_Click(object sender, EventArgs e)
        {
            this.ShowPreferencesForm();
        }

        private void MenuItemFileDirectories_Click(object sender, EventArgs e)
        {
            this.ShowDirectoriesForm();
        }

        private void MenuItemFileOpenSettings_Click(object sender, EventArgs e)
        {
            var prefsDir = Path.GetDirectoryName(Resources.UserPrefsFilePathV2);
            OpenHelper.OpenDirectory(prefsDir);
        }

        private void MenuItemFileClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void MenuItemHelpManual_Click(object sender, EventArgs e)
        {
            OpenHelper.OpenWebAddress(Resources.UrlHelp);
        }

        private void MenuItemHelpPortForwarding_Click(object sender, EventArgs e)
        {
            OpenHelper.OpenWebAddress(Resources.UrlPortForwardingGuide);
        }

        private void MenuItemHelpBugReport_Click(object sender, EventArgs e)
        {
            var bugReportForm = FormProvider.GetForm<BugReportForm>();
            bugReportForm.ShowDialog();
        }

        private void MenuItemHelpUpdates_Click(object sender, EventArgs e)
        {
            this.CheckForUpdates(true);
        }

        private void MenuItemHelpAbout_Click(object sender, EventArgs e)
        {
            var aboutForm = FormProvider.GetForm<AboutForm>();
            aboutForm.ShowDialog();
        }

        #endregion

        #region Form Field Events

        private void ButtonStartServer_Click()
        {
#if DEBUG
            if (SimulateStartServerException) throw new InvalidOperationException("Intentional exception thrown for testing");
#endif
            this.StartServer(true);
        }

        private void ButtonStopServer_Click()
        {
#if DEBUG
            if (SimulateStopServerException) throw new InvalidOperationException("Intentional exception thrown for testing");
#endif
            this.Server.Stop();
        }

        private void ButtonRestartServer_Click()
        {
            this.Server.Restart();
        }

        private void ButtonClearLogs_Click(object sender, EventArgs e)
        {
            this.ClearCurrentLogView();
        }

        private void ButtonSaveLogs_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(this.LogViewer.GetCurrentViewText()))
            {
                this.Logger.LogWarning("No logs to save!");
                return;
            }

            var dialog = new SaveFileDialog
            {
                FileName = $"{this.LogViewer.LogView}Logs-{DateTime.Now:u}.txt",
                Filter = "Text Files (*.txt)|*.txt",
                CheckPathExists = true,
                RestoreDirectory = true,
            };

            var result = dialog.ShowDialog();

            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.FileName))
            {
                try
                {
                    // Get the text again in case any new logs were written
                    File.WriteAllText(dialog.FileName, this.LogViewer.GetCurrentViewText());
                }
                catch (Exception exception)
                {
                    this.Logger.LogError(exception, $"Failed to write log file: {dialog.FileName}");
                }
            }
        }

        private void ButtonPlayerDetails_Click(object sender, EventArgs e)
        {
            if (!this.PlayersTable.TryGetSelectedRow<PlayerInfo>(out var row)) return;

            var form = this.FormProvider.GetForm<PlayerDetailsForm>();
            form.SetPlayerData(row.Entity);
            form.ShowDialog();
        }

        private void ButtonRemovePlayer_Click(object sender, EventArgs e)
        {
            if (this.PlayersTable.TryGetSelectedRow<PlayerInfo>(out var row))
            {
                this.PlayerDataProvider.Remove(row.Entity);
                this.PlayersTable.RemoveSelectedRow();
            }
        }

        private void ServerNameField_Changed(object sender, string value)
        {
            this.RefreshFormStateForServer();
        }

        private void ShowPasswordField_Changed(object sender, bool value)
        {
            this.ServerPasswordField.HideValue = !value;
        }

        private void NotifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.RefocusWindow();
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

        private void WorldSelectExistingNameField_EnabledChanged(object sender, EventArgs e)
        {
            this.RefreshWorldSelect();
        }

        private void TabPlayers_VisibleChanged(object sender, EventArgs e)
        {
            if (!this.TabPlayers.Visible) return;

            this.RefreshPlayersTable();
        }

        private void TabServerDetails_VisibleChanged()
        {
            if (!this.TabServerDetails.Visible) return;

            this.RefreshServerDetails();
            this.RefreshIpPorts();
        }

        private void LogViewSelectField_Changed(object sender, string viewName)
        {
            this.LogViewer.LogView = viewName;
        }

        private void ServerRefreshTimer_Tick(object sender, EventArgs e)
        {
            if (this.TabPlayers.Visible) this.RefreshPlayersTable();
            if (this.TabServerDetails.Visible) this.RefreshServerDetails();
        }

        private void UpdateCheckTimer_Tick()
        {
            this.CheckForUpdates(false);
        }

        private void PlayersTable_SelectionChanged(object sender, EventArgs e)
        {
            var isSelected = this.PlayersTable.TryGetSelectedRow<PlayerInfo>(out var row);
            this.ButtonPlayerDetails.Enabled = isSelected;
            this.ButtonRemovePlayer.Enabled = isSelected && row.Entity.PlayerStatus == PlayerStatus.Offline;
        }

        private void StatusStripLabelRight_Click()
        {
            if (!this.StatusStripLabelRight.IsLink) return;

            this.CheckForUpdates(true);
        }

        private void TrayContextMenuServerName_Click(object sender, EventArgs e)
        {
            this.RefocusWindow();
        }

        #endregion

        #region Service Events

        private void OnApplicationLogReceived(EventLogContext logEvent)
        {
            this.AddLog(logEvent.Message, LogViewApplication);
        }

        private void OnServerLogReceived(EventLogContext logEvent)
        {
            this.AddLog(logEvent.Message, LogViewServer);
        }

        private void OnServerStatusChanged(ServerStatus status)
        {
            this.SetStatusTextLeft(status.ToString(), this.ServerStatusIconMap[status]);

            this.RefreshFormStateForServer();

            if (status == ServerStatus.Running && this.WorldSelectRadioNew.Value)
            {
                // Once a "new world" starts running, switch back to the Existing Worlds screen
                // and select the newly created world
                this.RefreshWorldSelect();
                this.WorldSelectRadioExisting.Value = true;
            }

            if (status == ServerStatus.Running)
            {
                this.ServerUptimeTimer.Restart();
            }
            else
            {
                if (this.ServerUptimeTimer.IsRunning) this.ServerUptimeTimer.Stop();
            }

            if (status == ServerStatus.Stopped)
            {
                // Invite codes are only good for the session, clear it out when it's done
                this.SetInviteCode(null);
            }
            else if (status == ServerStatus.Starting && this.ServerCrossplayField.Value)
            {
                this.SetInviteCode("Loading...", false);
            }
        }

        private void OnPlayerUpdated(PlayerInfo player)
        {
            this.SetPlayerStatus(player);
        }

        private void OnWorldSaved(decimal duration)
        {
            this.LabelLastWorldSave.Value = $"{DateTime.Now:G} ({duration:F}ms)";

            if (this.WorldSaveTimes.Count >= 10) this.WorldSaveTimes.Dequeue();
            this.WorldSaveTimes.Enqueue(duration);

            var average = this.WorldSaveTimes.Average();
            this.LabelAverageWorldSave.Value = $"{average:F}ms";
        }

        private void OnInviteCodeReady(string inviteCode)
        {
            this.SetInviteCode(inviteCode);
        }

        private void IpAddressProvider_ExternalIpChanged(string ip)
        {
            this.LabelExternalIpAddress.Value = ip;
            this.RefreshIpPorts();
        }

        private void IpAddressProvider_InternalIpChanged(string ip)
        {
            this.LabelInternalIpAddress.Value = ip;
            this.RefreshIpPorts();
        }

        private void SoftwareUpdateProvider_UpdateCheckStarted()
        {
            this.SetStatusTextRight("Checking for updates...", Resources.Loading_Blue_16x, false);
        }

        private void SoftwareUpdateProvider_UpdateCheckFinished(SoftwareUpdateEventArgs e)
        {
            string manualCheckMessage;
            MessageBoxIcon manualCheckIcon;

            if (!e.IsSuccessful)
            {
                this.SetStatusTextRight($"Update check failed", Resources.StatusCriticalError_16x, true);

                var exception = e.Exception.GetPrimaryException();
                manualCheckMessage = $"Update check failed: {exception.Message}.";
                manualCheckIcon = MessageBoxIcon.Error;
            }
            else
            {
                var versionCompareResult = AssemblyHelper.CompareVersion(e.LatestVersion);

                if (versionCompareResult > 0)
                {
                    this.SetStatusTextRight($"Update available ({e.LatestVersion})", Resources.StatusWarning_16x, true);
                    manualCheckMessage = "A newer version of ValheimServerGUI is available.";
                    manualCheckIcon = MessageBoxIcon.Warning;
                }
                else if (versionCompareResult == 0)
                {
                    this.SetStatusTextRight($"Up to date ({e.LatestVersion})", Resources.StatusOK_16x, false);
                    manualCheckMessage = "You are running the latest version of ValheimServerGUI.";
                    manualCheckIcon = MessageBoxIcon.Question;
                }
                else if (versionCompareResult == -1)
                {
                    this.SetStatusTextRight($"Pre-release build ({AssemblyHelper.GetApplicationVersion()})", Resources.StatusOK_16x, false);
                    manualCheckMessage = "You are currently running a pre-release version of ValheimServerGUI. " +
                        $"The latest stable version is ({e.LatestVersion}).";
                    manualCheckIcon = MessageBoxIcon.Question;
                }
                else
                {
                    this.SetStatusTextRight($"Unable to parse version ({e.LatestVersion})", Resources.StatusCriticalError_16x, true);
                    manualCheckMessage = $"Update check failed: Unable to parse version ({e.LatestVersion}).";
                    manualCheckIcon = MessageBoxIcon.Error;
                }
            }

            if (e.IsManualCheck)
            {
                var result = MessageBox.Show(
                    $"{manualCheckMessage}{Environment.NewLine}Would you like to go to the download page?",
                    "Check for Updates",
                    MessageBoxButtons.YesNo,
                    manualCheckIcon);

                if (result == DialogResult.Yes)
                {
                    OpenHelper.OpenWebAddress(Resources.UrlUpdates);
                }
            }
        }

        private void OnPreferencesSaved(List<ServerPreferences> prefs)
        {
            this.RefreshProfileList();
        }

        #endregion

        #region Form Links

        private void ShowPreferencesForm()
        {
            this.FormProvider.GetForm<PreferencesForm>().ShowDialog();
            this.RefreshFormFields();
        }

        private void ShowDirectoriesForm()
        {
            this.FormProvider.GetForm<DirectoriesForm>().ShowDialog();
            this.RefreshFormFields();
        }

        #endregion

        #region View Setters

        private void AddLog(string message, string viewName)
        {
            this.LogViewer.AddLogToView(message, viewName);
        }

        private void ClearCurrentLogView()
        {
            this.LogViewer.ClearLogView(this.LogViewer.LogView);
        }

        private void SetInviteCode(string inviteCode, bool copyable = true)
        {
            if (string.IsNullOrWhiteSpace(inviteCode))
            {
                this.LabelInviteCode.Value = "N/A";
                this.CopyButtonInviteCode.Visible = false;
                return;
            }

            this.LabelInviteCode.Value = inviteCode;
            this.CopyButtonInviteCode.Visible = copyable;
        }

        private void SetPlayerStatus(PlayerInfo player)
        {
            var playerRows = this.PlayersTable
                .GetRowsWithType<PlayerInfo>()
                .Where(p => p.Entity.SteamId == player.SteamId);

            var playerRow = playerRows.FirstOrDefault(p => p.Entity.Key == player.Key) ?? this.PlayersTable.AddRowFromEntity(player);
            if (playerRow == null) return;

            // Update styles based on player status
            var imageIndex = -1;
            var color = this.PlayersTable.ForeColor;

            switch (player.PlayerStatus)
            {
                case PlayerStatus.Online:
                    imageIndex = this.GetImageIndex(nameof(Resources.StatusOK_16x));
                    break;
                case PlayerStatus.Joining:
                    imageIndex = this.GetImageIndex(nameof(Resources.UnsyncedCommits_16x_Horiz));
                    break;
                case PlayerStatus.Leaving:
                    imageIndex = this.GetImageIndex(nameof(Resources.UnsyncedCommits_16x_Horiz));
                    break;
                case PlayerStatus.Offline:
                    imageIndex = this.GetImageIndex(nameof(Resources.StatusNotStarted_16x));
                    color = Color.Gray;
                    break;
            }

            playerRow.ImageIndex = imageIndex;
            playerRow.ForeColor = color;
        }

        private void SetStatusTextLeft(string message, Image icon)
        {
            this.StatusStripLabelLeft.Text = message;
            this.StatusStripLabelLeft.Image = icon;
        }

        private void SetStatusTextRight(string message, Image icon, bool isLink)
        {
            this.StatusStripLabelRight.Text = message;
            this.StatusStripLabelRight.Image = icon;
            this.StatusStripLabelRight.IsLink = isLink;
        }

        #endregion

        #region View Refreshers

        private void RefreshFormFields()
        {
            this.RefreshProfileList();
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
            this.ServerCrossplayField.Enabled = allowServerChanges;
            this.ServerSaveIntervalField.Enabled = allowServerChanges;
            this.ServerBackupsField.Enabled = allowServerChanges;
            this.ServerShortBackupIntervalField.Enabled = allowServerChanges;
            this.ServerLongBackupIntervalField.Enabled = allowServerChanges;
            this.ServerAutoStartField.Enabled = allowServerChanges;
            this.ServerAdditionalArgsField.Enabled = allowServerChanges;

            this.MenuItemFileNewProfile.Enabled = allowServerChanges;
            this.MenuItemFileLoadProfile.Enabled = allowServerChanges;

            this.ButtonStartServer.Enabled = this.Server.CanStart;
            this.ButtonRestartServer.Enabled = this.Server.CanRestart;
            this.ButtonStopServer.Enabled = this.Server.CanStop;

            // Tray items are enabled based on their button equivalents
            this.TrayContextMenuStart.Enabled = this.ButtonStartServer.Enabled;
            this.TrayContextMenuRestart.Enabled = this.ButtonRestartServer.Enabled;
            this.TrayContextMenuStop.Enabled = this.ButtonStopServer.Enabled;

            this.TrayContextMenuServerName.Image = this.ServerStatusIconMap[this.Server.Status];
        }

        private void RefreshIpPorts()
        {
            const string ipExpr = @"^([\d]{1,3}\.[\d]{1,3}\.[\d]{1,3}\.[\d]{1,3})";

            var fields = new[] { this.LabelExternalIpAddress, this.LabelInternalIpAddress, this.LabelLocalIpAddress };
            var destPort = this.ServerPortField.Value;
            var isDefaultPort = destPort.ToString() == Resources.DefaultServerPort;

            foreach (var field in fields)
            {
                if (field.Value == null || field.Value == IpLoadingText) continue; // Don't try to modify loading text

                var ipMatch = Regex.Match(field.Value, ipExpr);
                var captures = (ipMatch.Groups as IEnumerable<Group>).Skip(1).Select(g => g.ToString()).ToArray();

                if (captures.Length == 0) continue; // Quit if we can't extract the IP address

                var ip = captures[0];
                field.Value = isDefaultPort ? ip : $"{ip}:{destPort}"; // Only append the port if it's not the default
            }
        }

        private void RefreshProfileList()
        {
            this.MenuItemFileLoadProfile.Enabled = false;
            this.MenuItemFileLoadProfile.DropDownItems.Clear();

            this.MenuItemFileRemoveProfile.Enabled = false;
            this.MenuItemFileRemoveProfile.DropDownItems.Clear();

            var prefs = this.ServerPrefsProvider.LoadPreferences()
                .OrderByDescending(p => p.LastSaved);
            if (prefs == null || !prefs.Any()) return;

            this.MenuItemFileLoadProfile.Enabled = true;
            this.MenuItemFileRemoveProfile.Enabled = true;

            foreach (var pref in prefs)
            {
                this.MenuItemFileLoadProfile.DropDownItems.Add(
                    pref.ProfileName,
                    null,
                    this.MenuItemFileLoadProfileItem_Click);
                this.MenuItemFileRemoveProfile.DropDownItems.Add(
                    pref.ProfileName,
                    null,
                    this.MenuItemFileRemoveProfileItem_Click);
            }
        }

        private void RefreshPlayersTable()
        {
            foreach (var row in this.PlayersTable.GetRowsWithType<PlayerInfo>())
            {
                row.RefreshValues();
            }
        }

        private void RefreshServerDetails()
        {
            if (this.Server.Status == ServerStatus.Running && this.ServerUptimeTimer != null)
            {
                var elapsed = this.ServerUptimeTimer.Elapsed;
                var days = elapsed.Days;
                var timestr = elapsed.ToString(@"hh\:mm\:ss");

                if (days == 1) timestr = $"1 day + {timestr}";
                else if (days > 1) timestr = $"{days} days + {timestr}";

                this.LabelSessionDuration.Value = timestr;
            }
        }

        private void RefreshWorldSelect()
        {
            // Don't change the dropdown unless it's in a state where it can be modified
            if (!this.WorldSelectExistingNameField.Enabled) return;

            try
            {
                // Refresh the existing worlds list, then re-select whatever was originally selected
                var selectedWorld = this.WorldSelectExistingNameField.Value;
                var worlds = this.FileProvider.GetWorldNames();

                this.WorldSelectExistingNameField.DataSource = worlds;
                this.WorldSelectExistingNameField.DropdownEnabled = worlds.Any();
                this.WorldSelectExistingNameField.Value = selectedWorld;
            }
            catch (Exception e)
            {
                // Show no worlds if something goes wrong
                this.WorldSelectExistingNameField.DataSource = null;
                this.WorldSelectExistingNameField.DropdownEnabled = false;
                this.Logger.LogError("Error refreshing world select: {message}", e.Message);
            }
        }

        private void RefreshCurrentProfileDisplayed()
        {
            if (this.CurrentProfile == null)
            {
                this.Text = Resources.ApplicationTitle;
                this.NotifyIcon.Text = "ValheimServerGUI";
                this.TrayContextMenuServerName.Text = "No Profile Selected";
            }
            else
            {
                this.Text = $"{Resources.ApplicationTitle} - {this.CurrentProfile}";
                this.NotifyIcon.Text = $"ValheimServerGUI - {this.CurrentProfile}";
                this.TrayContextMenuServerName.Text = $"Profile: {this.CurrentProfile}";
            }
        }

        #endregion

        #region Save & Load

        private ServerPreferences GetPrefsFromFormState()
        {
            var profileName = this.CurrentProfile;

            // Update existing prefs if they exist with this server name
            // Otherwise, create new prefs with this profile name
            var prefs = this.ServerPrefsProvider.LoadPreferences(profileName)
                ?? new ServerPreferences { ProfileName = profileName };

            prefs.Name = this.ServerNameField.Value;
            prefs.Port = this.ServerPortField.Value;
            prefs.Password = this.ServerPasswordField.Value;
            prefs.WorldName = this.WorldSelectRadioNew.Value
                ? this.WorldSelectNewNameField.Value
                : this.WorldSelectExistingNameField.Value;
            prefs.Public = this.CommunityServerField.Value;
            prefs.Crossplay = this.ServerCrossplayField.Value;
            prefs.SaveInterval = this.ServerSaveIntervalField.Value;
            prefs.BackupCount = this.ServerBackupsField.Value;
            prefs.BackupIntervalShort = this.ServerShortBackupIntervalField.Value;
            prefs.BackupIntervalLong = this.ServerLongBackupIntervalField.Value;
            prefs.AutoStart = this.ServerAutoStartField.Value;
            prefs.AdditionalArgs = this.ServerAdditionalArgsField.Value;

            return prefs;
        }

        private ServerPreferences SetFormStateFromPrefs(string profileName)
        {
            var prefs = this.ServerPrefsProvider.LoadPreferences(profileName);
            if (prefs == null)
            {
                this.Logger.LogWarning("Unable to set form state: no server profile exists with name '{name}'", profileName);
                return prefs;
            }

            this.SetFormStateFromPrefs(prefs);
            return prefs;
        }

        private void SetFormStateFromPrefs(ServerPreferences prefs)
        {
            if (prefs == null)
            {
                this.Logger.LogWarning($"Unable to set form state: {nameof(prefs)} cannot be null");
                return;
            }

            this.CurrentProfile = prefs.ProfileName;

            this.ServerNameField.Value = prefs.Name;
            this.ServerPortField.Value = prefs.Port;
            this.ServerPasswordField.Value = prefs.Password;
            this.ShowPasswordField.Value = false;
            this.CommunityServerField.Value = prefs.Public;
            this.ServerCrossplayField.Value = prefs.Crossplay;
            this.ServerSaveIntervalField.Value = prefs.SaveInterval;
            this.ServerBackupsField.Value = prefs.BackupCount;
            this.ServerShortBackupIntervalField.Value = prefs.BackupIntervalShort;
            this.ServerLongBackupIntervalField.Value = prefs.BackupIntervalLong;
            this.ServerAutoStartField.Value = prefs.AutoStart;
            this.ServerAdditionalArgsField.Value = prefs.AdditionalArgs;

            var worldName = prefs.WorldName;

            if (this.WorldSelectExistingNameField.DataSource != null &&
                this.WorldSelectExistingNameField.DataSource.Contains(worldName))
            {
                this.WorldSelectExistingNameField.Value = worldName;
                this.WorldSelectRadioExisting.Value = true;
            }
            else
            {
                this.WorldSelectNewNameField.Value = worldName;
                this.WorldSelectRadioNew.Value = true;
            }
        }

        #endregion

        #region Feature Capabilities

        private void StartServer(bool isManualStart)
        {
            var onError = (string message) =>
            {
                if (isManualStart)
                {
                    MessageBox.Show(message, "Error starting server", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    this.Logger.LogError("Error starting server: {message}", message);
                }
            };

            var portFieldValue = this.ServerPortField.Value;
            if (!this.IpAddressProvider.IsLocalUdpPortAvailable(portFieldValue, portFieldValue + 1))
            {
                onError($"Port {portFieldValue} or {portFieldValue + 1} is already in use.{NL}" +
                    $"Valheim requires two adjacent ports to run a dedicated server.{NL}" +
                    "Please shut down any UDP applications using these ports, or choose a different port for your server.");
                return;
            }

            string worldName;
            bool newWorld = this.WorldSelectRadioNew.Value;

            if (newWorld)
            {
                // Creating a new world, ensure that the name is available
                worldName = this.WorldSelectNewNameField.Value;
                if (string.IsNullOrWhiteSpace(worldName))
                {
                    onError("You must enter a world name, or choose an existing world.");
                    return;
                }

                if (!this.FileProvider.IsWorldNameAvailable(worldName))
                {
                    onError($"A world named '{worldName}' already exists.");
                    this.WorldSelectRadioExisting.Value = true;
                    this.WorldSelectExistingNameField.Value = worldName;
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
                    onError($"No world exists with name '{worldName}'.");
                    return;
                }
            }

            var userPrefs = this.UserPrefsProvider.LoadPreferences();
            var serverPrefs = this.GetPrefsFromFormState();

            var options = new ValheimServerOptions
            {
                Name = serverPrefs.Name,
                Password = serverPrefs.Password,
                WorldName = worldName, // Server automatically creates a new world if a world doesn't yet exist w/ that name
                NewWorld = newWorld,
                Public = serverPrefs.Public,
                Port = serverPrefs.Port,
                Crossplay = serverPrefs.Crossplay,
                SaveInterval = serverPrefs.SaveInterval,
                Backups = serverPrefs.BackupCount,
                BackupShort = serverPrefs.BackupIntervalShort,
                BackupLong = serverPrefs.BackupIntervalLong,
                AdditionalArgs = serverPrefs.AdditionalArgs,
            };

            try
            {
                options.Validate();
                Server.Start(options);
            }
            catch (Exception exception)
            {
                onError(exception.Message);
                return;
            }

            if (userPrefs.SaveProfileOnStart)
            {
                this.ServerPrefsProvider.SavePreferences(serverPrefs);
            }
        }

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

        private string PromptForProfileName(string startingText = null)
        {
            var dialog = new TextPromptPopout("Server Profile Name", "Enter a server profile name:", startingText);
            dialog.SetValidation(
                "Profile name must be 1-30 characters, and must not match an existing profile name.",
                (input) =>
                {
                    if (string.IsNullOrWhiteSpace(input) || input.Length > 30) return false;

                    var prefs = this.ServerPrefsProvider.LoadPreferences(input);
                    return prefs == null;
                });

            var result = dialog.ShowDialog();
            if (result != DialogResult.OK) return null;

            return dialog.Value;
        }

        private void LaunchNewWindow()
        {
            var splashForm = this.FormProvider.GetForm<SplashForm>();
            var mainWindow = splashForm.CreateNewMainWindow(this.CurrentProfile, false);
            mainWindow.Show();
        }

        private void RefocusWindow()
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
            }

            this.Activate();
        }

        private void CheckForUpdates(bool isManualCheck)
        {
            Task.Run(() => this.SoftwareUpdateProvider.CheckForUpdatesAsync(isManualCheck));
        }

        private void CloseWindowOnServerStopped()
        {
            if (this.Server.IsAnyStatus(ServerStatus.Stopped))
            {
                this.Close();
                return;
            }

            this.Server.StatusChanged += this.BuildEventHandler<ServerStatus>((status) =>
            {
                if (status == ServerStatus.Stopped)
                {
                    this.Close();
                }
            });
        }

        #endregion

        #region Helper Methods

        private int GetImageIndex(string key)
        {
            return this.ImageList.Images.IndexOfKey(key);
        }

        #endregion
    }
}
