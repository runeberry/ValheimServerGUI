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
using ValheimServerGUI.Tools.Preferences;

namespace ValheimServerGUI.Forms
{
    public partial class MainWindow : Form
    {
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

        private readonly TimeSpan UpdateCheckInterval = TimeSpan.Parse(Resources.UpdateCheckInterval);
        private DateTime NextUpdateCheck = DateTime.MaxValue;

        private readonly IFormProvider FormProvider;
        private readonly IUserPreferences UserPrefs;
        private readonly IValheimFileProvider FileProvider;
        private readonly IPlayerDataRepository PlayerDataProvider;
        private readonly ValheimServer Server;
        private readonly ValheimServerLogger ServerLogger;
        private readonly IEventLogger Logger;
        private readonly IIpAddressProvider IpAddressProvider;
        private readonly IGitHubClient GitHubClient;

        public MainWindow(
            IFormProvider formProvider,
            IUserPreferences userPrefs,
            IValheimFileProvider fileProvider,
            IPlayerDataRepository playerDataProvider,
            ValheimServer server,
            ValheimServerLogger serverLogger,
            IEventLogger appLogger,
            IIpAddressProvider ipAddressProvider,
            IGitHubClient gitHubClient)
        {
            this.FormProvider = formProvider;
            this.UserPrefs = userPrefs;
            this.FileProvider = fileProvider;
            this.PlayerDataProvider = playerDataProvider;
            this.Server = server;
            this.ServerLogger = serverLogger;
            this.Logger = appLogger;
            this.IpAddressProvider = ipAddressProvider;
            this.GitHubClient = gitHubClient;

            InitializeComponent(); // WinForms generated code, always first
            InitializeImages();
            InitializeServer();
            InitializeFormEvents();
            InitializeFormFields(); // Display data back to user, always last

            this.PlayerDataProvider.Load(); // todo: find a better way
        }

        #region Initialization

        private void InitializeImages()
        {
            this.ImageList.AddImagesFromResourceFile(typeof(Resources));
        }

        private void InitializeServer()
        {
            this.Logger.LogReceived += this.BuildEventHandler<EventLogContext>(this.OnApplicationLogReceived);
            this.ServerLogger.LogReceived += this.BuildEventHandler<EventLogContext>(this.OnServerLogReceived);
            this.Server.StatusChanged += this.BuildEventHandler<ServerStatus>(this.OnServerStatusChanged);
            this.Server.WorldSaved += this.BuildEventHandler<decimal>(this.OnWorldSaved);

            this.PlayerDataProvider.DataReady += this.BuildEventHandler(this.OnPlayerDataReady);
            this.PlayerDataProvider.EntityUpdated += this.BuildEventHandler<PlayerInfo>(this.OnPlayerUpdated);

            this.IpAddressProvider.ExternalIpReceived += this.BuildEventHandler<string>(this.IpAddressProvider_ExternalIpReceived);
            this.IpAddressProvider.InternalIpReceived += this.BuildEventHandler<string>(this.IpAddressProvider_InternalIpReceived);
        }

        private void InitializeFormEvents()
        {
            // MainWindow
            this.Shown += this.BuildEventHandlerAsync(this.MainWindow_Load, 250);

            // Menu items
            this.MenuItemFileDirectories.Click += this.MenuItemFileDirectories_Clicked;
            this.MenuItemFileClose.Click += this.MenuItemFileClose_Clicked;
            this.MenuItemHelpManual.Click += this.MenuItemHelpManual_Click;
            this.MenuItemHelpPortForwarding.Click += this.MenuItemHelpPortForwarding_Clicked;
            this.MenuItemHelpUpdates.Click += this.BuildEventHandlerAsync(this.MenuItemHelpUpdates_Clicked);
            this.MenuItemHelpAbout.Click += this.MenuItemHelpAbout_Clicked;

            // Tray icon
            this.NotifyIcon.MouseClick += this.NotifyIcon_MouseClick;
            this.TrayContextMenuStart.Click += this.ButtonStartServer_Click;
            this.TrayContextMenuRestart.Click += this.ButtonRestartServer_Click;
            this.TrayContextMenuStop.Click += this.ButtonStopServer_Click;
            this.TrayContextMenuClose.Click += this.MenuItemFileClose_Clicked;

            // Timers
            this.ServerRefreshTimer.Tick += this.ServerRefreshTimer_Tick;
            this.UpdateCheckTimer.Tick += this.BuildEventHandlerAsync(this.UpdateCheckTimer_Tick);

            // Tabs
            this.TabPlayers.VisibleChanged += this.TabPlayers_VisibleChanged;
            this.TabServerDetails.VisibleChanged += this.BuildEventHandler(this.TabServerDetails_VisibleChanged);

            // Buttons
            this.ButtonStartServer.Click += this.ButtonStartServer_Click;
            this.ButtonRestartServer.Click += this.ButtonRestartServer_Click;
            this.ButtonStopServer.Click += this.ButtonStopServer_Click;
            this.ButtonClearLogs.Click += this.ButtonClearLogs_Click;
            this.ButtonSaveLogs.Click += this.ButtonSaveLogs_Click;
            this.ButtonPlayerDetails.Click += this.ButtonPlayerDetails_Click;
            this.ButtonRemovePlayer.Click += this.ButtonRemovePlayer_Click;
            this.CopyButtonExternalIpAddress.CopyFunction = () => this.LabelExternalIpAddress.Value;
            this.CopyButtonInternalIpAddress.CopyFunction = () => this.LabelInternalIpAddress.Value;
            this.CopyButtonLocalIpAddress.CopyFunction = () => this.LabelLocalIpAddress.Value;
            this.StatusStripLabelRight.Click += this.BuildEventHandlerAsync(this.StatusStripLabelRight_Click);

            // Form fields
            this.ShowPasswordField.ValueChanged += this.ShowPasswordField_Changed;
            this.WorldSelectRadioExisting.ValueChanged += this.WorldSelectRadioExisting_Changed;
            this.WorldSelectRadioNew.ValueChanged += this.WorldSelectRadioNew_Changed;
            this.LogViewSelectField.ValueChanged += this.LogViewSelectField_Changed;
            this.PlayersTable.SelectionChanged += this.PlayersTable_SelectionChanged;
        }

        private void InitializeFormFields()
        {
            this.PlayersTable.AddRowBinding<PlayerInfo>(row =>
            {
                row.AddCellBinding(this.ColumnPlayerName.Index, p => p.PlayerName ?? $"(...{p.SteamId[^4..]})");
                row.AddCellBinding(this.ColumnPlayerStatus.Index, p => p.PlayerStatus);
                row.AddCellBinding(this.ColumnPlayerUpdated.Index, p => new TimeAgo(p.LastStatusChange));
            });

            this.LogViewSelectField.DataSource = new[] { LogViewServer, LogViewApplication };
            this.LogViewSelectField.Value = LogViewServer;

            this.RefreshFormFields();
            this.LoadFormValuesFromUserPrefs();
            this.OnServerStatusChanged(ServerStatus.Stopped);
        }

        #endregion

        #region MainWindow Events

        private Task MainWindow_Load()
        {
            this.Logger.LogInformation($"Valheim Server GUI v{AssemblyHelper.GetApplicationVersion()} - Loaded OK");

            return Task.WhenAll(
                this.RefreshInternalIpAsync(),
                this.RefreshExternalIpAsync(),
                this.CheckForUpdatesAsync(false)
            );
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            this.CheckFilePaths();
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

        private void MenuItemHelpPortForwarding_Clicked(object sender, EventArgs e)
        {
            WebHelper.OpenWebAddress(Resources.UrlPortForwardingGuide);
        }

        private async Task MenuItemHelpUpdates_Clicked()
        {
            await this.CheckForUpdatesAsync(true);
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
            bool newWorld = this.WorldSelectRadioNew.Value;

            if (newWorld)
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
                NewWorld = newWorld,
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

        private async Task UpdateCheckTimer_Tick()
        {
            if (DateTime.UtcNow > this.NextUpdateCheck)
            {
                await this.CheckForUpdatesAsync(false);
            }
        }

        private void PlayersTable_SelectionChanged(object sender, EventArgs e)
        {
            var isSelected = this.PlayersTable.TryGetSelectedRow<PlayerInfo>(out var row);
            this.ButtonPlayerDetails.Enabled = isSelected;
            this.ButtonRemovePlayer.Enabled = isSelected && row.Entity.PlayerStatus == PlayerStatus.Offline;
        }

        private async Task StatusStripLabelRight_Click()
        {
            if (!this.StatusStripLabelRight.IsLink) return;

            await this.CheckForUpdatesAsync(true);
        }

        #endregion

        #region Server Events

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
        }

        private void OnPlayerDataReady()
        {
            foreach (var player in this.PlayerDataProvider.Data)
            {
                this.UpdatePlayerStatus(player);
            }
        }

        private void OnPlayerUpdated(PlayerInfo player)
        {
            this.UpdatePlayerStatus(player);
        }

        private void OnWorldSaved(decimal duration)
        {
            this.LabelLastWorldSave.Value = $"{DateTime.Now:G} ({duration:F}ms)";

            if (this.WorldSaveTimes.Count >= 10) this.WorldSaveTimes.Dequeue();
            this.WorldSaveTimes.Enqueue(duration);
            
            var average = this.WorldSaveTimes.Average();
            this.LabelAverageWorldSave.Value = $"{average:F}ms";
        }

        private void IpAddressProvider_ExternalIpReceived(string ip)
        {
            this.LabelExternalIpAddress.Value = ip;
            this.RefreshIpPorts();
        }

        private void IpAddressProvider_InternalIpReceived(string ip)
        {
            this.LabelInternalIpAddress.Value = ip;
            this.RefreshIpPorts();
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

        private void AddLog(string message, string viewName)
        {
            this.LogViewer.AddLogToView(message, viewName);
        }

        private void ClearCurrentLogView()
        {
            this.LogViewer.ClearLogView(this.LogViewer.LogView);
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

        private int GetImageIndex(string key)
        {
            return this.ImageList.Images.IndexOfKey(key);
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

        private async Task RefreshExternalIpAsync()
        {
            if (this.LabelExternalIpAddress.Value == IpLoadingText) await this.IpAddressProvider.GetExternalIpAddressAsync();
        }

        private async Task RefreshInternalIpAsync()
        {
            if (this.LabelInternalIpAddress.Value == IpLoadingText) await this.IpAddressProvider.GetInternalIpAddressAsync();
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

        private void UpdatePlayerStatus(PlayerInfo player)
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

        private async Task CheckForUpdatesAsync(bool showPrompt)
        {
            this.SetStatusTextRight("Checking for updates...", Resources.Loading_Blue_16x, false);

            var currentVersion = AssemblyHelper.GetApplicationVersion();
            var release = await this.GitHubClient.GetLatestReleaseAsync();

            if (AssemblyHelper.IsNewerVersion(release?.TagName))
            {
                this.SetStatusTextRight($"Update available ({release.TagName})", Resources.StatusWarning_16x, true);

                if (showPrompt)
                {
                    var result = MessageBox.Show(
                        $"A newer version of ValheimServerGUI is available." + Environment.NewLine +
                        "Would you like to go to the download page?",
                        "Check for Updates",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.Yes)
                    {
                        WebHelper.OpenWebAddress(Resources.UrlUpdates);
                    }
                }
            }
            else
            {
                currentVersion = release.TagName ?? currentVersion; // Use the v-prefixed version if available
                this.SetStatusTextRight($"Up to date ({currentVersion})", Resources.StatusOK_16x, false);

                if (showPrompt)
                {
                    var result = MessageBox.Show(
                        "You are running the latest version of ValheimServerGUI." + Environment.NewLine +
                        "Would you like to go to the download page?",
                        "Check for Updates",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        WebHelper.OpenWebAddress(Resources.UrlUpdates);
                    }
                }
            }

            this.NextUpdateCheck = DateTime.UtcNow + this.UpdateCheckInterval;
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
