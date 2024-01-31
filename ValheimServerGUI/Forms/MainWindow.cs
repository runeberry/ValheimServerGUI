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
using ValheimServerGUI.Tools.Models;

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
            get => _currentProfile;
            set
            {
                _currentProfile = value;
                ProfileChanged?.Invoke(this, _currentProfile);
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
        private readonly IWorldPreferencesProvider WorldPrefsProvider;
        private readonly IPlayerDataRepository PlayerDataProvider;
        private readonly ValheimServer Server;
        private readonly IApplicationLogger Logger;
        private readonly IIpAddressProvider IpAddressProvider;
        private readonly ISoftwareUpdateProvider SoftwareUpdateProvider;

        public MainWindow(
            IFormProvider formProvider,
            IUserPreferencesProvider userPrefsProvider,
            IServerPreferencesProvider serverPrefsProvider,
            IWorldPreferencesProvider worldPrefsProvider,
            IPlayerDataRepository playerDataProvider,
            ValheimServer server,
            IApplicationLogger appLogger,
            IIpAddressProvider ipAddressProvider,
            ISoftwareUpdateProvider softwareUpdateProvider)
        {
#if DEBUG
            if (SimulateConstructorException) throw new InvalidOperationException("Intentional exception thrown for testing");
#endif
            FormProvider = formProvider;
            UserPrefsProvider = userPrefsProvider;
            ServerPrefsProvider = serverPrefsProvider;
            WorldPrefsProvider = worldPrefsProvider;
            PlayerDataProvider = playerDataProvider;
            Server = server;
            Logger = appLogger;
            IpAddressProvider = ipAddressProvider;
            SoftwareUpdateProvider = softwareUpdateProvider;

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
            ImageList.AddImagesFromResourceFile(typeof(Resources));
        }

        private void InitializeServices()
        {
            Server.StatusChanged += this.BuildEventHandler<ServerStatus>(OnServerStatusChanged);
            Server.WorldSaved += this.BuildEventHandler<decimal>(OnWorldSaved);
            Server.InviteCodeReady += this.BuildEventHandler<string>(OnInviteCodeReady);

            PlayerDataProvider.EntityUpdated += this.BuildEventHandler<PlayerInfo>(OnPlayerUpdated);

            IpAddressProvider.ExternalIpChanged += this.BuildEventHandler<string>(IpAddressProvider_ExternalIpChanged);
            IpAddressProvider.InternalIpChanged += this.BuildEventHandler<string>(IpAddressProvider_InternalIpChanged);

            SoftwareUpdateProvider.UpdateCheckStarted += this.BuildEventHandler(SoftwareUpdateProvider_UpdateCheckStarted);
            SoftwareUpdateProvider.UpdateCheckFinished += this.BuildEventHandler<SoftwareUpdateEventArgs>(SoftwareUpdateProvider_UpdateCheckFinished);

            ServerPrefsProvider.PreferencesSaved += this.BuildEventHandler<List<ServerPreferences>>(OnPreferencesSaved);
        }

        private void InitializeFormEvents()
        {
            // MainWindow
            ProfileChanged += this.BuildEventHandler<string>(OnProfileChanged);

            // Menu items
            MenuItemFileNewWindow.Click += MenuItemFileNewWindow_Click;
            MenuItemFileNewProfile.Click += MenuItemFileNewProfile_Click;
            MenuItemFileSaveProfile.Click += MenuItemFileSaveProfile_Click;
            MenuItemFileSaveProfileAs.Click += MenuItemFileSaveProfileAs_Click;
            MenuItemFilePreferences.Click += MenuItemFilePreferences_Click;
            MenuItemFileDirectories.Click += MenuItemFileDirectories_Click;
            MenuItemFileOpenSettings.Click += MenuItemFileOpenSettings_Click;
            MenuItemFileClose.Click += MenuItemFileClose_Click;
            MenuItemHelpManual.Click += MenuItemHelpManual_Click;
            MenuItemHelpPortForwarding.Click += MenuItemHelpPortForwarding_Click;
            MenuItemHelpBugReport.Click += MenuItemHelpBugReport_Click;
            MenuItemHelpUpdates.Click += MenuItemHelpUpdates_Click;
            MenuItemHelpDiscord.Click += MenuItemHelpDiscord_Click;
            MenuItemHelpAbout.Click += MenuItemHelpAbout_Click;

            // Tray icon
            NotifyIcon.MouseClick += NotifyIcon_MouseClick;
            TrayContextMenuServerName.Click += TrayContextMenuServerName_Click;
            TrayContextMenuStart.Click += this.BuildEventHandler(ButtonStartServer_Click);
            TrayContextMenuRestart.Click += this.BuildEventHandler(ButtonRestartServer_Click);
            TrayContextMenuStop.Click += this.BuildEventHandler(ButtonStopServer_Click);
            TrayContextMenuClose.Click += MenuItemFileClose_Click;

            // Timers
            ServerRefreshTimer.Tick += ServerRefreshTimer_Tick;
            UpdateCheckTimer.Tick += this.BuildEventHandler(UpdateCheckTimer_Tick);

            // Tabs
            TabPlayers.VisibleChanged += TabPlayers_VisibleChanged;
            TabServerDetails.VisibleChanged += this.BuildEventHandler(TabServerDetails_VisibleChanged);

            // Buttons
            ButtonStartServer.Click += this.BuildEventHandler(ButtonStartServer_Click);
            ButtonRestartServer.Click += this.BuildEventHandler(ButtonRestartServer_Click);
            ButtonStopServer.Click += this.BuildEventHandler(ButtonStopServer_Click);
            ButtonClearLogs.Click += ButtonClearLogs_Click;
            ButtonSaveLogs.Click += ButtonSaveLogs_Click;
            LogsFolderOpenButton.PathFunction = () => Resources.LogsFolderPath;
            ButtonPlayerDetails.Click += ButtonPlayerDetails_Click;
            LinkCharacterNamesHelp.Click += LinkCharacterNamesHelp_Click;
            ButtonRemovePlayer.Click += ButtonRemovePlayer_Click;
            CopyButtonServerPassword.CopyFunction = () => ServerPasswordField.Value;
            WorldsListSettingsButton.ClickFunction = WorldsListSettingsButton_Click;
            WorldsListRefreshButton.RefreshFunction = WorldsListRefreshButton_Click;
            WorldsFolderOpenButton.PathFunction = () => GetServerOptionsFromFormState().SaveDataFolderPath;
            CopyButtonExternalIpAddress.CopyFunction = () => LabelExternalIpAddress.Value;
            CopyButtonInternalIpAddress.CopyFunction = () => LabelInternalIpAddress.Value;
            CopyButtonLocalIpAddress.CopyFunction = () => LabelLocalIpAddress.Value;
            CopyButtonInviteCode.CopyFunction = () => LabelInviteCode.Value;
            ServerExePathOpenButton.PathFunction = () => ServerExePathField.Value;
            ServerSaveDataPathOpenButton.PathFunction = () => ServerSaveDataFolderPathField.Value;
            StatusStripLabelRight.Click += this.BuildEventHandler(StatusStripLabelRight_Click);

            // Form fields
            ServerNameField.ValueChanged += ServerNameField_Changed;
            ShowPasswordField.ValueChanged += ShowPasswordField_Changed;
            WorldSelectRadioExisting.ValueChanged += WorldSelectRadioExisting_Changed;
            WorldSelectRadioNew.ValueChanged += WorldSelectRadioNew_Changed;
            WorldSelectExistingNameField.EnabledChanged += WorldSelectExistingNameField_EnabledChanged;
            LogViewSelectField.ValueChanged += LogViewSelectField_Changed;
            PlayersTable.SelectionChanged += PlayersTable_SelectionChanged;
        }

        private void InitializeFormFields()
        {
            // Write message backlog to application log view...
            foreach (var message in Logger.LogBuffer)
            {
                LogViewer.AddLogToView(message, LogViews.Application);
            }
            // ...then write all new messages to that log view.
            Logger.LogReceived += this.BuildActionHandler<string>(OnApplicationLogReceived);

            LogViewSelectField.DataSource = new[] { LogViews.Server, LogViews.Application };
            LogViewSelectField.Value = LogViews.Server;
            ServerExePathField.ConfigureFileDialog(dialog => dialog.Filter = "Applications (*.exe)|*.exe");

            RefreshFormFields();
        }

        private void InitializeStartupPrefs()
        {
            // If StartProfile was set by SplashForm before this window was shown, then
            // load that server profile up now, and start the server is settings indicate that.
            if (StartProfile != null)
            {
                var serverPrefs = SetFormStateFromPrefs(StartProfile);
                if (serverPrefs != null && StartServerAutomatically)
                {
                    StartServer(false);
                }
                else
                {
                    OnServerStatusChanged(ServerStatus.Stopped);
                }
            }
            else
            {
                // No server to start, mock a "stopped" event to initialize the form
                OnServerStatusChanged(ServerStatus.Stopped);
            }
        }

        private void InitializeIpAddresses()
        {
            var internalIp = IpAddressProvider.InternalIpAddress;
            var externalIp = IpAddressProvider.ExternalIpAddress;

            LabelInternalIpAddress.Value = internalIp ?? IpLoadingText;
            LabelExternalIpAddress.Value = externalIp ?? IpLoadingText;

            if (internalIp == null) IpAddressProvider.LoadInternalIpAddressAsync();
            if (externalIp == null) IpAddressProvider.LoadExternalIpAddressAsync();

            RefreshIpPorts();
        }

        private void InitializePlayerData()
        {
            PlayersTable.AddRowBinding<PlayerInfo>(row =>
            {
                row.AddCellBinding(ColumnPlayerName.Index, GetPlayerDisplayName);
                row.AddCellBinding(ColumnPlayerStatus.Index, p => p.PlayerStatus);
                row.AddCellBinding(ColumnPlayerUpdated.Index, p => new TimeAgo(p.LastStatusChange));
            });

            foreach (var playerInfo in PlayerDataProvider.Data)
            {
                SetPlayerStatus(playerInfo);
            }
        }

        #endregion

        #region MainWindow Events

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            InitializeIpAddresses();
            InitializePlayerData();
            InitializeStartupPrefs();
            CheckFilePaths();
            NotifyIcon.Visible = true;

            Logger.Information("Valheim Server GUI v{version} - Loaded OK", AssemblyHelper.GetApplicationVersion());
        }

        private void OnProfileChanged(string _)
        {
            RefreshCurrentProfileDisplayed();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (WindowState == FormWindowState.Minimized)
            {
                ShowInTaskbar = false;
            }
            else
            {
                ShowInTaskbar = true;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (e.CloseReason == CloseReason.UserClosing)
            {
                if (Server.IsAnyStatus(ServerStatus.Starting, ServerStatus.Running))
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
                        Server.Stop();
                        CloseWindowOnServerStopped();
                    }

                    // Cancel the form close regardless of the user's choice
                    e.Cancel = true;
                }
                else if (Server.IsAnyStatus(ServerStatus.Stopping))
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
                Server.Stop();
                CloseWindowOnServerStopped();
                e.Cancel = true;
            }
            else
            {
                // Try to stop the server, but don't wait around for the results
                Server.Stop();
            }
        }

        #endregion

        #region Menu Items

        private void MenuItemFileNewWindow_Click(object sender, EventArgs e)
        {
            LaunchNewWindow();
        }

        private void MenuItemFileNewProfile_Click(object sender, EventArgs e)
        {
            var profileName = PromptForProfileName();
            if (profileName == null) return;

            var prefs = new ServerPreferences { ProfileName = profileName, Name = profileName };
            ServerPrefsProvider.SavePreferences(prefs);

            SetFormStateFromPrefs(prefs);
        }

        private void MenuItemFileSaveProfile_Click(object sender, EventArgs e)
        {
            var prefs = GetPrefsFromFormState();
            ServerPrefsProvider.SavePreferences(prefs);
        }

        private void MenuItemFileSaveProfileAs_Click(object sender, EventArgs e)
        {
            var profileName = PromptForProfileName($"Copy of {CurrentProfile}");
            if (profileName == null) return;

            var prefs = GetPrefsFromFormState();
            prefs.ProfileName = profileName;
            ServerPrefsProvider.SavePreferences(prefs);
            CurrentProfile = profileName;
        }

        private void MenuItemFileLoadProfileItem_Click(object sender, EventArgs e)
        {
            if (sender is not ToolStripItem item) return;

            SetFormStateFromPrefs(item.Text);
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
                ServerPrefsProvider.RemovePreferences(profileName);
            }
        }

        private void MenuItemFilePreferences_Click(object sender, EventArgs e)
        {
            ShowPreferencesForm();
        }

        private void MenuItemFileDirectories_Click(object sender, EventArgs e)
        {
            ShowDirectoriesForm();
        }

        private void MenuItemFileOpenSettings_Click(object sender, EventArgs e)
        {
            var prefsDir = Path.GetDirectoryName(Resources.UserPrefsFilePathV2);
            OpenHelper.OpenDirectory(prefsDir);
        }

        private void MenuItemFileClose_Click(object sender, EventArgs e)
        {
            Close();
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
            CheckForUpdates(true);
        }

        private void MenuItemHelpDiscord_Click(object sender, EventArgs e)
        {
            OpenHelper.OpenWebAddress(Resources.UrlDiscord);
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
            StartServer(true);
        }

        private void ButtonStopServer_Click()
        {
#if DEBUG
            if (SimulateStopServerException) throw new InvalidOperationException("Intentional exception thrown for testing");
#endif
            Server.Stop();
        }

        private void ButtonRestartServer_Click()
        {
            Server.Restart();
        }

        private void ButtonClearLogs_Click(object sender, EventArgs e)
        {
            ClearCurrentLogView();
        }

        private void ButtonSaveLogs_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(LogViewer.GetCurrentViewText()))
            {
                Logger.Warning("No logs to save!");
                return;
            }

            string initialDirectory;
            try
            {
                initialDirectory = PathExtensions.GetDirectoryInfo(Resources.LogsFolderPath).FullName;
            }
            catch
            {
                initialDirectory = null;
            }

            var dialog = new SaveFileDialog
            {
                FileName = $"{PathExtensions.GetValidFileName(LogViewer.LogView, true)}.txt",
                Filter = "Text Files (*.txt)|*.txt",
                CheckPathExists = true,
                RestoreDirectory = true,
                InitialDirectory = initialDirectory,
            };

            var result = dialog.ShowDialog();

            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.FileName))
            {
                try
                {
                    // Get the text again in case any new logs were written
                    File.WriteAllText(dialog.FileName, LogViewer.GetCurrentViewText());
                }
                catch (Exception exception)
                {
                    Logger.Error(exception, "Failed to write log file: {fileName}", dialog.FileName);
                }
            }
        }

        private void ButtonPlayerDetails_Click(object sender, EventArgs e)
        {
            if (!PlayersTable.TryGetSelectedRow<PlayerInfo>(out var row)) return;

            var form = FormProvider.GetForm<PlayerDetailsForm>();
            form.SetPlayerData(row.Entity);
            form.ShowDialog();
        }

        private void LinkCharacterNamesHelp_Click(object sender, EventArgs e)
        {
            OpenHelper.OpenWebAddress(Resources.UrlHelpCharacterNames);
        }

        private void ButtonRemovePlayer_Click(object sender, EventArgs e)
        {
            if (PlayersTable.TryGetSelectedRow<PlayerInfo>(out var row))
            {
                PlayerDataProvider.Remove(row.Entity);
                PlayersTable.RemoveSelectedRow();
            }
        }

        private void ServerNameField_Changed(object sender, string value)
        {
            RefreshFormStateForServer();
        }

        private void ShowPasswordField_Changed(object sender, bool value)
        {
            ServerPasswordField.HideValue = !value;
        }

        private void NotifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                RefocusWindow();
            }
        }

        private void WorldSelectRadioExisting_Changed(object sender, bool value)
        {
            WorldSelectExistingNameField.Visible = value;

            if (value)
            {
                // When going back to the Existing World field, select the New Name if it's already an existing world
                WorldSelectExistingNameField.Value = WorldSelectNewNameField.Value;

                // Then, empty out the new name field
                WorldSelectNewNameField.Value = null;
            }
        }

        private void WorldSelectRadioNew_Changed(object sender, bool value)
        {
            WorldSelectNewNameField.Visible = value;
        }

        private void WorldSelectExistingNameField_EnabledChanged(object sender, EventArgs e)
        {
            RefreshWorldSelect();
        }

        private void TabPlayers_VisibleChanged(object sender, EventArgs e)
        {
            if (!TabPlayers.Visible) return;

            RefreshPlayersTable();
        }

        private void TabServerDetails_VisibleChanged()
        {
            if (!TabServerDetails.Visible) return;

            RefreshServerDetails();
            RefreshIpPorts();
        }

        private void LogViewSelectField_Changed(object sender, string viewName)
        {
            LogViewer.LogView = viewName;
        }

        private void ServerRefreshTimer_Tick(object sender, EventArgs e)
        {
            if (TabPlayers.Visible) RefreshPlayersTable();
            if (TabServerDetails.Visible) RefreshServerDetails();
        }

        private void UpdateCheckTimer_Tick()
        {
            CheckForUpdates(false);
        }

        private void PlayersTable_SelectionChanged(object sender, EventArgs e)
        {
            var isSelected = PlayersTable.TryGetSelectedRow<PlayerInfo>(out var row);
            ButtonPlayerDetails.Enabled = isSelected;
            ButtonRemovePlayer.Enabled = isSelected && row.Entity.PlayerStatus == PlayerStatus.Offline;
        }

        private void StatusStripLabelRight_Click()
        {
            if (!StatusStripLabelRight.IsLink) return;

            CheckForUpdates(true);
        }

        private void TrayContextMenuServerName_Click(object sender, EventArgs e)
        {
            RefocusWindow();
        }

        private void WorldsListSettingsButton_Click()
        {
            string worldName;

            if (WorldSelectRadioNew.Value)
            {
                worldName = WorldSelectNewNameField.Value;

                if (string.IsNullOrWhiteSpace(worldName))
                {
                    MessageBox.Show(
                        "Please enter a new world name before changing modifier settings.",
                        "World Name Missing",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
            }
            else
            {
                worldName = WorldSelectExistingNameField.Value;

                if (string.IsNullOrWhiteSpace(worldName))
                {
                    MessageBox.Show(
                        "Unable to change modifier settings. No world is selected.",
                        "World Name Missing",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
            }

            var form = FormProvider.GetForm<WorldPreferencesForm>();
            form.SetWorld(worldName);
            form.ShowDialog();
        }

        private void WorldsListRefreshButton_Click()
        {
            RefreshWorldSelect();

            if (!WorldSelectRadioExisting.Value)
            {
                // Switch to the world list after refreshing it
                WorldSelectRadioExisting.Value = true;
            }
        }

        #endregion

        #region Service Events

        private void OnApplicationLogReceived(string message)
        {
            LogViewer.AddLogToView(message, LogViews.Application);
        }

        private void OnServerLogReceived(string message)
        {
            LogViewer.AddLogToView(message, LogViews.Server);
        }

        private void OnServerStatusChanged(ServerStatus status)
        {
            SetStatusTextLeft(status.ToString(), ServerStatusIconMap[status]);

            RefreshFormStateForServer();

            if (status == ServerStatus.Running && WorldSelectRadioNew.Value)
            {
                // Once a "new world" starts running, switch back to the Existing Worlds screen
                // and select the newly created world
                RefreshWorldSelect();
                var worldName = WorldSelectNewNameField.Value;
                WorldSelectRadioExisting.Value = true;
                WorldSelectExistingNameField.Value = worldName;
            }

            if (status == ServerStatus.Running)
            {
                ServerUptimeTimer.Restart();
            }
            else
            {
                if (ServerUptimeTimer.IsRunning) ServerUptimeTimer.Stop();
            }

            if (status == ServerStatus.Stopped)
            {
                // Invite codes are only good for the session, clear it out when it's done
                SetInviteCode(null);
            }
            else if (status == ServerStatus.Starting && ServerCrossplayField.Value)
            {
                SetInviteCode("Loading...", false);
            }
        }

        private void OnPlayerUpdated(PlayerInfo player)
        {
            SetPlayerStatus(player);
        }

        private void OnWorldSaved(decimal duration)
        {
            LabelLastWorldSave.Value = $"{DateTime.Now:G} ({duration:F}ms)";

            if (WorldSaveTimes.Count >= 10) WorldSaveTimes.Dequeue();
            WorldSaveTimes.Enqueue(duration);

            var average = WorldSaveTimes.Average();
            LabelAverageWorldSave.Value = $"{average:F}ms";
        }

        private void OnInviteCodeReady(string inviteCode)
        {
            SetInviteCode(inviteCode);
        }

        private void IpAddressProvider_ExternalIpChanged(string ip)
        {
            LabelExternalIpAddress.Value = ip;
            RefreshIpPorts();
        }

        private void IpAddressProvider_InternalIpChanged(string ip)
        {
            LabelInternalIpAddress.Value = ip;
            RefreshIpPorts();
        }

        private void SoftwareUpdateProvider_UpdateCheckStarted()
        {
            SetStatusTextRight("Checking for updates...", Resources.Loading_Blue_16x, false);
        }

        private void SoftwareUpdateProvider_UpdateCheckFinished(SoftwareUpdateEventArgs e)
        {
            string manualCheckMessage;
            MessageBoxIcon manualCheckIcon;

            if (!e.IsSuccessful)
            {
                SetStatusTextRight($"Update check failed", Resources.StatusCriticalError_16x, true);

                var exception = e.Exception.GetPrimaryException();
                manualCheckMessage = $"Update check failed: {exception.Message}.";
                manualCheckIcon = MessageBoxIcon.Error;
            }
            else
            {
                var versionCompareResult = AssemblyHelper.CompareVersion(e.LatestVersion);

                if (versionCompareResult > 0)
                {
                    SetStatusTextRight($"Update available ({e.LatestVersion})", Resources.StatusWarning_16x, true);
                    manualCheckMessage = "A newer version of ValheimServerGUI is available.";
                    manualCheckIcon = MessageBoxIcon.Warning;
                }
                else if (versionCompareResult == 0)
                {
                    SetStatusTextRight($"Up to date ({e.LatestVersion})", Resources.StatusOK_16x, false);
                    manualCheckMessage = "You are running the latest version of ValheimServerGUI.";
                    manualCheckIcon = MessageBoxIcon.Question;
                }
                else if (versionCompareResult == -1)
                {
                    SetStatusTextRight($"Pre-release build ({AssemblyHelper.GetApplicationVersion()})", Resources.StatusOK_16x, false);
                    manualCheckMessage = "You are currently running a pre-release version of ValheimServerGUI. " +
                        $"The latest stable version is ({e.LatestVersion}).";
                    manualCheckIcon = MessageBoxIcon.Question;
                }
                else
                {
                    SetStatusTextRight($"Unable to parse version ({e.LatestVersion})", Resources.StatusCriticalError_16x, true);
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
            RefreshProfileList();
        }

        #endregion

        #region Form Links

        private void ShowPreferencesForm()
        {
            FormProvider.GetForm<PreferencesForm>().ShowDialog();
            RefreshFormFields();
        }

        private void ShowDirectoriesForm()
        {
            FormProvider.GetForm<DirectoriesForm>().ShowDialog();
            RefreshFormFields();
        }

        #endregion

        #region View Setters

        private void ClearCurrentLogView()
        {
            LogViewer.ClearLogView(LogViewer.LogView);
        }

        private void SetInviteCode(string inviteCode, bool copyable = true)
        {
            if (string.IsNullOrWhiteSpace(inviteCode))
            {
                LabelInviteCode.Value = "N/A";
                CopyButtonInviteCode.Visible = false;
                return;
            }

            LabelInviteCode.Value = inviteCode;
            CopyButtonInviteCode.Visible = copyable;
        }

        private void SetPlayerStatus(PlayerInfo player)
        {
            var playerRows = PlayersTable
                .GetRowsWithType<PlayerInfo>()
                .Where(r => r.Entity.Platform == player.Platform && r.Entity.PlayerId == player.PlayerId);

            var playerRow = playerRows.FirstOrDefault(p => p.Entity.Key == player.Key) ?? PlayersTable.AddRowFromEntity(player);
            if (playerRow == null) return;

            // Update styles based on player status and platform
            var platformImageIndex = -1;
            var color = PlayersTable.ForeColor;

            switch (player.Platform)
            {
                case PlayerPlatforms.Steam:
                    platformImageIndex = GetImageIndex(nameof(Resources.Steam_16x));
                    break;
                case PlayerPlatforms.Xbox:
                    platformImageIndex = GetImageIndex(nameof(Resources.XboxLive_16x));
                    break;
            }

            switch (player.PlayerStatus)
            {
                case PlayerStatus.Offline:
                    color = Color.Gray;
                    break;
            }

            playerRow.ImageIndex = platformImageIndex;
            playerRow.ForeColor = color;
        }

        private void SetStatusTextLeft(string message, Image icon)
        {
            StatusStripLabelLeft.Text = message;
            StatusStripLabelLeft.Image = icon;
        }

        private void SetStatusTextRight(string message, Image icon, bool isLink)
        {
            StatusStripLabelRight.Text = message;
            StatusStripLabelRight.Image = icon;
            StatusStripLabelRight.IsLink = isLink;
        }

        #endregion

        #region View Refreshers

        private void RefreshFormFields()
        {
            RefreshProfileList();
            RefreshWorldSelect();
            RefreshFormStateForServer();
        }

        private void RefreshFormStateForServer()
        {
            // Only allow form field changes when the server is stopped
            bool allowServerChanges = Server.Status == ServerStatus.Stopped;

            ServerNameField.Enabled = allowServerChanges;
            ServerPortField.Enabled = allowServerChanges;
            ServerPasswordField.Enabled = allowServerChanges;
            WorldSelectRadioNew.Enabled = allowServerChanges;
            WorldSelectRadioExisting.Enabled = allowServerChanges;
            WorldSelectExistingNameField.Enabled = allowServerChanges;
            WorldSelectNewNameField.Enabled = allowServerChanges;
            CommunityServerField.Enabled = allowServerChanges;
            ServerCrossplayField.Enabled = allowServerChanges;
            ServerSaveIntervalField.Enabled = allowServerChanges;
            ServerBackupsField.Enabled = allowServerChanges;
            ServerShortBackupIntervalField.Enabled = allowServerChanges;
            ServerLongBackupIntervalField.Enabled = allowServerChanges;
            ServerAutoStartField.Enabled = allowServerChanges;
            ServerAdditionalArgsField.Enabled = allowServerChanges;
            ServerExePathField.Enabled = allowServerChanges;
            ServerSaveDataFolderPathField.Enabled = allowServerChanges;
            ServerLogFileField.Enabled = allowServerChanges;

            MenuItemFileNewProfile.Enabled = allowServerChanges;
            MenuItemFileLoadProfile.Enabled = allowServerChanges;

            ButtonStartServer.Enabled = Server.CanStart;
            ButtonRestartServer.Enabled = Server.CanRestart;
            ButtonStopServer.Enabled = Server.CanStop;

            // Tray items are enabled based on their button equivalents
            TrayContextMenuStart.Enabled = ButtonStartServer.Enabled;
            TrayContextMenuRestart.Enabled = ButtonRestartServer.Enabled;
            TrayContextMenuStop.Enabled = ButtonStopServer.Enabled;

            TrayContextMenuServerName.Image = ServerStatusIconMap[Server.Status];
        }

        private void RefreshIpPorts()
        {
            const string ipExpr = @"^([\d]{1,3}\.[\d]{1,3}\.[\d]{1,3}\.[\d]{1,3})";

            var fields = new[] { LabelExternalIpAddress, LabelInternalIpAddress, LabelLocalIpAddress };
            var destPort = ServerPortField.Value;
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
            MenuItemFileLoadProfile.Enabled = false;
            MenuItemFileLoadProfile.DropDownItems.Clear();

            MenuItemFileRemoveProfile.Enabled = false;
            MenuItemFileRemoveProfile.DropDownItems.Clear();

            var prefs = ServerPrefsProvider.LoadPreferences()
                .OrderByDescending(p => p.LastSaved);
            if (prefs == null || !prefs.Any()) return;

            MenuItemFileLoadProfile.Enabled = true;
            MenuItemFileRemoveProfile.Enabled = true;

            foreach (var pref in prefs)
            {
                MenuItemFileLoadProfile.DropDownItems.Add(
                    pref.ProfileName,
                    null,
                    MenuItemFileLoadProfileItem_Click);
                MenuItemFileRemoveProfile.DropDownItems.Add(
                    pref.ProfileName,
                    null,
                    MenuItemFileRemoveProfileItem_Click);
            }
        }

        private void RefreshPlayersTable()
        {
            foreach (var row in PlayersTable.GetRowsWithType<PlayerInfo>())
            {
                row.RefreshValues();
            }
        }

        private void RefreshServerDetails()
        {
            if (Server.Status == ServerStatus.Running && ServerUptimeTimer != null)
            {
                var elapsed = ServerUptimeTimer.Elapsed;
                var days = elapsed.Days;
                var timestr = elapsed.ToServerElapsedFormat();

                if (days == 1) timestr = $"1 day + {timestr}";
                else if (days > 1) timestr = $"{days} days + {timestr}";

                LabelSessionDuration.Value = timestr;
            }
        }

        private void RefreshWorldSelect()
        {
            try
            {
                // Refresh the existing worlds list, then re-select whatever was originally selected
                var selectedWorld = WorldSelectExistingNameField.Value;
                var options = GetServerOptionsFromFormState();
                var worlds = options.GetValidatedSaveDataFolder().GetWorldNames();

                WorldSelectExistingNameField.DataSource = worlds;
                WorldSelectExistingNameField.DropdownEnabled = worlds.Any();
                WorldSelectExistingNameField.Value = selectedWorld;
            }
            catch (Exception e)
            {
                // Show no worlds if something goes wrong
                WorldSelectExistingNameField.DataSource = null;
                WorldSelectExistingNameField.DropdownEnabled = false;
                Logger.Error("Error refreshing world select: {message}", e.Message);
            }
        }

        private void RefreshCurrentProfileDisplayed()
        {
            if (CurrentProfile == null)
            {
                Text = Resources.ApplicationTitle;
                NotifyIcon.Text = "ValheimServerGUI";
                TrayContextMenuServerName.Text = "No Profile Selected";
            }
            else
            {
                Text = $"{Resources.ApplicationTitle} - {CurrentProfile}";
                NotifyIcon.Text = $"ValheimServerGUI - {CurrentProfile}";
                TrayContextMenuServerName.Text = $"Profile: {CurrentProfile}";
            }
        }

        #endregion

        #region Save & Load

        private ServerPreferences GetPrefsFromFormState()
        {
            var profileName = CurrentProfile;

            // Update existing prefs if they exist with this server name
            // Otherwise, create new prefs with this profile name
            var prefs = ServerPrefsProvider.LoadPreferences(profileName)
                ?? new ServerPreferences { ProfileName = profileName };

            prefs.Name = ServerNameField.Value;
            prefs.Port = ServerPortField.Value;
            prefs.Password = ServerPasswordField.Value;
            prefs.WorldName = WorldSelectRadioNew.Value
                ? WorldSelectNewNameField.Value
                : WorldSelectExistingNameField.Value;
            prefs.Public = CommunityServerField.Value;
            prefs.Crossplay = ServerCrossplayField.Value;
            prefs.SaveInterval = ServerSaveIntervalField.Value;
            prefs.BackupCount = ServerBackupsField.Value;
            prefs.BackupIntervalShort = ServerShortBackupIntervalField.Value;
            prefs.BackupIntervalLong = ServerLongBackupIntervalField.Value;
            prefs.AutoStart = ServerAutoStartField.Value;
            prefs.AdditionalArgs = ServerAdditionalArgsField.Value;
            prefs.ServerExePath = ServerExePathField.Value;
            prefs.SaveDataFolderPath = ServerSaveDataFolderPathField.Value;
            prefs.WriteServerLogsToFile = ServerLogFileField.Value;

            return prefs;
        }

        private ValheimServerOptions GetServerOptionsFromFormState()
        {
            var userPrefs = UserPrefsProvider.LoadPreferences();
            var serverPrefs = GetPrefsFromFormState();

            var options = new ValheimServerOptions
            {
                Name = serverPrefs.Name,
                Password = serverPrefs.Password,
                WorldName = serverPrefs.WorldName, // Server automatically creates a new world if a world doesn't yet exist w/ that name
                Public = serverPrefs.Public,
                Port = serverPrefs.Port,
                Crossplay = serverPrefs.Crossplay,
                SaveInterval = serverPrefs.SaveInterval,
                Backups = serverPrefs.BackupCount,
                BackupShort = serverPrefs.BackupIntervalShort,
                BackupLong = serverPrefs.BackupIntervalLong,
                AdditionalArgs = serverPrefs.AdditionalArgs,
                ServerExePath = !string.IsNullOrWhiteSpace(serverPrefs.ServerExePath)
                    ? serverPrefs.ServerExePath
                    : userPrefs.ServerExePath,
                SaveDataFolderPath = !string.IsNullOrWhiteSpace(serverPrefs.SaveDataFolderPath)
                    ? serverPrefs.SaveDataFolderPath
                    : userPrefs.SaveDataFolderPath,
                LogToFile = serverPrefs.WriteServerLogsToFile,
                LogMessageHandler = this.BuildActionHandler<string>(OnServerLogReceived),
            };

            // If a world is selected, load preferences for that world
            var worldName = serverPrefs.WorldName;
            if (!string.IsNullOrWhiteSpace(worldName))
            {
                var worldPrefs = WorldPrefsProvider.LoadPreferences(worldName);
                if (worldPrefs != null)
                {
                    if (!string.IsNullOrEmpty(worldPrefs.Preset))
                    {
                        options.WorldPreset = worldPrefs.Preset;
                    }
                    else
                    {
                        options.WorldModifiers = worldPrefs.Modifiers;
                    }

                    options.WorldKeys = worldPrefs.Keys;
                }
            }

            return options;
        }

        private ServerPreferences SetFormStateFromPrefs(string profileName)
        {
            var prefs = ServerPrefsProvider.LoadPreferences(profileName);
            if (prefs == null)
            {
                Logger.Warning("Unable to set form state: no server profile exists with name '{name}'", profileName);
                return prefs;
            }

            SetFormStateFromPrefs(prefs);
            return prefs;
        }

        private void SetFormStateFromPrefs(ServerPreferences prefs)
        {
            if (prefs == null)
            {
                Logger.Warning($"Unable to set form state: {nameof(prefs)} cannot be null");
                return;
            }

            CurrentProfile = prefs.ProfileName;

            ServerNameField.Value = prefs.Name;
            ServerPortField.Value = prefs.Port;
            ServerPasswordField.Value = prefs.Password;
            ShowPasswordField.Value = false;
            CommunityServerField.Value = prefs.Public;
            ServerCrossplayField.Value = prefs.Crossplay;
            ServerSaveIntervalField.Value = prefs.SaveInterval;
            ServerBackupsField.Value = prefs.BackupCount;
            ServerShortBackupIntervalField.Value = prefs.BackupIntervalShort;
            ServerLongBackupIntervalField.Value = prefs.BackupIntervalLong;
            ServerAutoStartField.Value = prefs.AutoStart;
            ServerAdditionalArgsField.Value = prefs.AdditionalArgs;
            ServerExePathField.Value = prefs.ServerExePath;
            ServerSaveDataFolderPathField.Value = prefs.SaveDataFolderPath;
            ServerLogFileField.Value = prefs.WriteServerLogsToFile;

            RefreshWorldSelect();
            var worldName = prefs.WorldName;

            if (WorldSelectExistingNameField.DataSource != null &&
                WorldSelectExistingNameField.DataSource.Contains(worldName))
            {
                WorldSelectExistingNameField.Value = worldName;
                WorldSelectRadioExisting.Value = true;
            }
            else
            {
                WorldSelectNewNameField.Value = worldName;
                WorldSelectRadioNew.Value = true;
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
                    Logger.Error("Error starting server: {message}", message);
                }
            };

            var options = GetServerOptionsFromFormState();

            // Run standard input validation first
            try
            {
                options.Validate();
            }
            catch (Exception exception)
            {
                onError(exception.Message);
                return;
            }

            // Then, run additional validation that requires more context than just the fields themselves
            var port = options.Port;
            if (!IpAddressProvider.IsLocalUdpPortAvailable(port, port + 1))
            {
                onError($"Port {port} or {port + 1} is already in use.{NL}" +
                    $"Valheim requires two adjacent ports to run a dedicated server.{NL}" +
                    "Please shut down any UDP applications using these ports, or choose a different port for your server.");
                return;
            }

            var worldName = options.WorldName;
            var saveFolder = options.GetValidatedSaveDataFolder();
            bool newWorld = WorldSelectRadioNew.Value;

            if (newWorld)
            {
                // Creating a new world, ensure that the name is available
                if (string.IsNullOrWhiteSpace(worldName))
                {
                    onError("You must enter a world name, or choose an existing world.");
                    return;
                }

                if (worldName.Length < 5 || worldName.Length > 20)
                {
                    onError("World name must be 5-20 characters long.");
                    return;
                }

                if (!saveFolder.IsWorldNameAvailable(worldName))
                {
                    onError($"A world named '{worldName}' already exists.");
                    WorldSelectRadioExisting.Value = true;
                    WorldSelectExistingNameField.Value = worldName;
                    return;
                }
            }
            else
            {
                // Using an existing world, ensure that the file exists
                if (saveFolder.IsWorldNameAvailable(worldName))
                {
                    // Don't think this is possible to hit because the name comes from a dropdown
                    onError($"No world exists with name '{worldName}'.");
                    return;
                }
            }

            // Finally, after all validation has finished, try to start the server
            try
            {
                Server.Start(options);
            }
            catch (Exception exception)
            {
                onError(exception.Message);
                return;
            }

            var userPrefs = UserPrefsProvider.LoadPreferences();
            if (userPrefs.SaveProfileOnStart)
            {
                var serverPrefs = GetPrefsFromFormState();
                ServerPrefsProvider.SavePreferences(serverPrefs);
            }
        }

        private void CheckFilePaths()
        {
            try
            {
                var options = GetServerOptionsFromFormState();
                options.GetValidatedServerExe();
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
                    ShowDirectoriesForm();
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

                    var prefs = ServerPrefsProvider.LoadPreferences(input);
                    return prefs == null;
                });

            var result = dialog.ShowDialog();
            if (result != DialogResult.OK) return null;

            return dialog.Value;
        }

        private void LaunchNewWindow()
        {
            var splashForm = FormProvider.GetForm<SplashForm>();
            var mainWindow = splashForm.CreateNewMainWindow(CurrentProfile, false);
            mainWindow.Show();
        }

        private void RefocusWindow()
        {
            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }

            Activate();
        }

        private void CheckForUpdates(bool isManualCheck)
        {
            Task.Run(() => SoftwareUpdateProvider.CheckForUpdatesAsync(isManualCheck));
        }

        private void CloseWindowOnServerStopped()
        {
            if (Server.IsAnyStatus(ServerStatus.Stopped))
            {
                Close();
                return;
            }

            Server.StatusChanged += this.BuildEventHandler<ServerStatus>((status) =>
            {
                if (status == ServerStatus.Stopped)
                {
                    Close();
                }
            });
        }

        #endregion

        #region Helper Methods

        private int GetImageIndex(string key)
        {
            return ImageList.Images.IndexOfKey(key);
        }

        private string GetPlayerDisplayName(PlayerInfo player)
        {
            // Show the last 4 digits of the player's platform ID if their name is not yet known
            var name = player.PlayerName ?? $"[...{player.PlayerId[^4..]}]";

            if (!string.IsNullOrWhiteSpace(player.LastStatusCharacter))
            {
                name += $" ({player.LastStatusCharacter})";
            }

            return name;
        }

        #endregion
    }
}
