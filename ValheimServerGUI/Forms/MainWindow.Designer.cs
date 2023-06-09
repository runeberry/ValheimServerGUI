
namespace ValheimServerGUI.Forms
{
    partial class MainWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
            MenuStrip = new System.Windows.Forms.MenuStrip();
            MenuItemFile = new System.Windows.Forms.ToolStripMenuItem();
            MenuItemFileNewWindow = new System.Windows.Forms.ToolStripMenuItem();
            MenuItemFileSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            MenuItemFileNewProfile = new System.Windows.Forms.ToolStripMenuItem();
            MenuItemFileSaveProfile = new System.Windows.Forms.ToolStripMenuItem();
            MenuItemFileSaveProfileAs = new System.Windows.Forms.ToolStripMenuItem();
            MenuItemFileLoadProfile = new System.Windows.Forms.ToolStripMenuItem();
            MenuItemFileRemoveProfile = new System.Windows.Forms.ToolStripMenuItem();
            MenuItemFileSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            MenuItemFilePreferences = new System.Windows.Forms.ToolStripMenuItem();
            MenuItemFileDirectories = new System.Windows.Forms.ToolStripMenuItem();
            MenuItemFileOpenSettings = new System.Windows.Forms.ToolStripMenuItem();
            MenuItemFileSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            MenuItemFileClose = new System.Windows.Forms.ToolStripMenuItem();
            MenuItemHelp = new System.Windows.Forms.ToolStripMenuItem();
            MenuItemHelpManual = new System.Windows.Forms.ToolStripMenuItem();
            MenuItemHelpPortForwarding = new System.Windows.Forms.ToolStripMenuItem();
            MenuItemHelpBugReport = new System.Windows.Forms.ToolStripMenuItem();
            MenuItemHelpSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            MenuItemHelpUpdates = new System.Windows.Forms.ToolStripMenuItem();
            MenuItemHelpDiscord = new System.Windows.Forms.ToolStripMenuItem();
            MenuItemHelpAbout = new System.Windows.Forms.ToolStripMenuItem();
            StatusStrip = new System.Windows.Forms.StatusStrip();
            StatusStripLabelLeft = new System.Windows.Forms.ToolStripStatusLabel();
            StatusStripLabelSpacer = new System.Windows.Forms.ToolStripStatusLabel();
            StatusStripLabelRight = new System.Windows.Forms.ToolStripStatusLabel();
            Tabs = new System.Windows.Forms.TabControl();
            TabServerControls = new System.Windows.Forms.TabPage();
            CopyButtonServerPassword = new CopyButton();
            JoinOptionsGroupBox = new System.Windows.Forms.GroupBox();
            CommunityServerField = new ValheimServerGUI.Controls.CheckboxFormField();
            ServerCrossplayField = new ValheimServerGUI.Controls.CheckboxFormField();
            WorldSelectGroupBox = new System.Windows.Forms.GroupBox();
            WorldsListRefreshButton = new RefreshButton();
            WorldsFolderOpenButton = new OpenButton();
            WorldSelectNewNameField = new Controls.TextFormField();
            WorldSelectRadioNew = new ValheimServerGUI.Controls.RadioFormField();
            WorldSelectRadioExisting = new ValheimServerGUI.Controls.RadioFormField();
            WorldSelectExistingNameField = new ValheimServerGUI.Controls.DropdownFormField();
            ServerPortField = new ValheimServerGUI.Controls.NumericFormField();
            ButtonRestartServer = new System.Windows.Forms.Button();
            ShowPasswordField = new ValheimServerGUI.Controls.CheckboxFormField();
            ServerPasswordField = new Controls.TextFormField();
            ServerNameField = new Controls.TextFormField();
            ButtonStopServer = new System.Windows.Forms.Button();
            ButtonStartServer = new System.Windows.Forms.Button();
            TabAdvancedControls = new System.Windows.Forms.TabPage();
            SavingGroupBox = new System.Windows.Forms.GroupBox();
            ServerLongBackupIntervalField = new ValheimServerGUI.Controls.NumericFormField();
            ServerSaveIntervalField = new ValheimServerGUI.Controls.NumericFormField();
            ServerShortBackupIntervalField = new ValheimServerGUI.Controls.NumericFormField();
            ServerBackupsField = new ValheimServerGUI.Controls.NumericFormField();
            OtherSettingsGroupBox = new System.Windows.Forms.GroupBox();
            ServerLogFileField = new ValheimServerGUI.Controls.CheckboxFormField();
            ServerAutoStartField = new ValheimServerGUI.Controls.CheckboxFormField();
            ServerAdditionalArgsField = new Controls.TextFormField();
            DirectoriesGroupBox = new System.Windows.Forms.GroupBox();
            ServerSaveDataPathOpenButton = new OpenButton();
            ServerExePathOpenButton = new OpenButton();
            ServerSaveDataFolderPathField = new ValheimServerGUI.Controls.FilenameFormField();
            ServerExePathField = new ValheimServerGUI.Controls.FilenameFormField();
            TabServerDetails = new System.Windows.Forms.TabPage();
            groupBox2 = new System.Windows.Forms.GroupBox();
            LabelSessionDuration = new ValheimServerGUI.Controls.LabelField();
            LabelAverageWorldSave = new ValheimServerGUI.Controls.LabelField();
            LabelLastWorldSave = new ValheimServerGUI.Controls.LabelField();
            groupBox1 = new System.Windows.Forms.GroupBox();
            CopyButtonInviteCode = new CopyButton();
            LabelInviteCode = new ValheimServerGUI.Controls.LabelField();
            CopyButtonLocalIpAddress = new CopyButton();
            CopyButtonExternalIpAddress = new CopyButton();
            CopyButtonInternalIpAddress = new CopyButton();
            label1 = new System.Windows.Forms.Label();
            LabelExternalIpAddress = new ValheimServerGUI.Controls.LabelField();
            LabelLocalIpAddress = new ValheimServerGUI.Controls.LabelField();
            LabelInternalIpAddress = new ValheimServerGUI.Controls.LabelField();
            TabPlayers = new System.Windows.Forms.TabPage();
            ButtonRemovePlayer = new System.Windows.Forms.Button();
            ButtonPlayerDetails = new System.Windows.Forms.Button();
            PlayersTable = new ValheimServerGUI.Controls.DataListView();
            ColumnPlayerName = new System.Windows.Forms.ColumnHeader();
            ColumnPlayerStatus = new System.Windows.Forms.ColumnHeader();
            ColumnPlayerUpdated = new System.Windows.Forms.ColumnHeader();
            ImageList = new System.Windows.Forms.ImageList(components);
            TabLogs = new System.Windows.Forms.TabPage();
            LogsFolderOpenButton = new OpenButton();
            ButtonSaveLogs = new System.Windows.Forms.Button();
            LogViewSelectField = new ValheimServerGUI.Controls.DropdownFormField();
            LogViewer = new ValheimServerGUI.Controls.LogViewer();
            ButtonClearLogs = new System.Windows.Forms.Button();
            NotifyIcon = new System.Windows.Forms.NotifyIcon(components);
            TrayContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(components);
            TrayContextMenuServerName = new System.Windows.Forms.ToolStripMenuItem();
            TrayContextMenuSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            TrayContextMenuStart = new System.Windows.Forms.ToolStripMenuItem();
            TrayContextMenuRestart = new System.Windows.Forms.ToolStripMenuItem();
            TrayContextMenuStop = new System.Windows.Forms.ToolStripMenuItem();
            TrayContextMenuSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            TrayContextMenuClose = new System.Windows.Forms.ToolStripMenuItem();
            ServerRefreshTimer = new System.Windows.Forms.Timer(components);
            UpdateCheckTimer = new System.Windows.Forms.Timer(components);
            MenuStrip.SuspendLayout();
            StatusStrip.SuspendLayout();
            Tabs.SuspendLayout();
            TabServerControls.SuspendLayout();
            JoinOptionsGroupBox.SuspendLayout();
            WorldSelectGroupBox.SuspendLayout();
            TabAdvancedControls.SuspendLayout();
            SavingGroupBox.SuspendLayout();
            OtherSettingsGroupBox.SuspendLayout();
            DirectoriesGroupBox.SuspendLayout();
            TabServerDetails.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox1.SuspendLayout();
            TabPlayers.SuspendLayout();
            TabLogs.SuspendLayout();
            TrayContextMenuStrip.SuspendLayout();
            SuspendLayout();
            // 
            // MenuStrip
            // 
            MenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { MenuItemFile, MenuItemHelp });
            MenuStrip.Location = new System.Drawing.Point(0, 0);
            MenuStrip.Name = "MenuStrip";
            MenuStrip.Size = new System.Drawing.Size(484, 24);
            MenuStrip.TabIndex = 0;
            // 
            // MenuItemFile
            // 
            MenuItemFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { MenuItemFileNewWindow, MenuItemFileSeparator3, MenuItemFileNewProfile, MenuItemFileSaveProfile, MenuItemFileSaveProfileAs, MenuItemFileLoadProfile, MenuItemFileRemoveProfile, MenuItemFileSeparator2, MenuItemFilePreferences, MenuItemFileDirectories, MenuItemFileOpenSettings, MenuItemFileSeparator1, MenuItemFileClose });
            MenuItemFile.Name = "MenuItemFile";
            MenuItemFile.Size = new System.Drawing.Size(37, 20);
            MenuItemFile.Text = "&File";
            // 
            // MenuItemFileNewWindow
            // 
            MenuItemFileNewWindow.Image = Properties.Resources.AddImmediateWindow_16x;
            MenuItemFileNewWindow.Name = "MenuItemFileNewWindow";
            MenuItemFileNewWindow.Size = new System.Drawing.Size(208, 22);
            MenuItemFileNewWindow.Text = "New &Window";
            // 
            // MenuItemFileSeparator3
            // 
            MenuItemFileSeparator3.Name = "MenuItemFileSeparator3";
            MenuItemFileSeparator3.Size = new System.Drawing.Size(205, 6);
            // 
            // MenuItemFileNewProfile
            // 
            MenuItemFileNewProfile.Image = Properties.Resources.NewFile_16x;
            MenuItemFileNewProfile.Name = "MenuItemFileNewProfile";
            MenuItemFileNewProfile.Size = new System.Drawing.Size(208, 22);
            MenuItemFileNewProfile.Text = "&New Profile";
            // 
            // MenuItemFileSaveProfile
            // 
            MenuItemFileSaveProfile.Image = Properties.Resources.Save_16x;
            MenuItemFileSaveProfile.Name = "MenuItemFileSaveProfile";
            MenuItemFileSaveProfile.Size = new System.Drawing.Size(208, 22);
            MenuItemFileSaveProfile.Text = "&Save Profile";
            // 
            // MenuItemFileSaveProfileAs
            // 
            MenuItemFileSaveProfileAs.Image = Properties.Resources.Save_16x;
            MenuItemFileSaveProfileAs.Name = "MenuItemFileSaveProfileAs";
            MenuItemFileSaveProfileAs.Size = new System.Drawing.Size(208, 22);
            MenuItemFileSaveProfileAs.Text = "Save Profile &As...";
            // 
            // MenuItemFileLoadProfile
            // 
            MenuItemFileLoadProfile.Image = Properties.Resources.OpenFile_16x;
            MenuItemFileLoadProfile.Name = "MenuItemFileLoadProfile";
            MenuItemFileLoadProfile.Size = new System.Drawing.Size(208, 22);
            MenuItemFileLoadProfile.Text = "&Load Profile";
            // 
            // MenuItemFileRemoveProfile
            // 
            MenuItemFileRemoveProfile.Image = Properties.Resources.Cancel_16x;
            MenuItemFileRemoveProfile.Name = "MenuItemFileRemoveProfile";
            MenuItemFileRemoveProfile.Size = new System.Drawing.Size(208, 22);
            MenuItemFileRemoveProfile.Text = "&Remove Profile";
            // 
            // MenuItemFileSeparator2
            // 
            MenuItemFileSeparator2.Name = "MenuItemFileSeparator2";
            MenuItemFileSeparator2.Size = new System.Drawing.Size(205, 6);
            // 
            // MenuItemFilePreferences
            // 
            MenuItemFilePreferences.Image = Properties.Resources.Settings_16x;
            MenuItemFilePreferences.Name = "MenuItemFilePreferences";
            MenuItemFilePreferences.Size = new System.Drawing.Size(208, 22);
            MenuItemFilePreferences.Text = "&Preferences...";
            // 
            // MenuItemFileDirectories
            // 
            MenuItemFileDirectories.Image = Properties.Resources.FolderInformation_16x;
            MenuItemFileDirectories.Name = "MenuItemFileDirectories";
            MenuItemFileDirectories.Size = new System.Drawing.Size(208, 22);
            MenuItemFileDirectories.Text = "Set &Directories...";
            // 
            // MenuItemFileOpenSettings
            // 
            MenuItemFileOpenSettings.Image = Properties.Resources.OpenFolder_16x;
            MenuItemFileOpenSettings.Name = "MenuItemFileOpenSettings";
            MenuItemFileOpenSettings.Size = new System.Drawing.Size(208, 22);
            MenuItemFileOpenSettings.Text = "&Open Settings Directory...";
            // 
            // MenuItemFileSeparator1
            // 
            MenuItemFileSeparator1.Name = "MenuItemFileSeparator1";
            MenuItemFileSeparator1.Size = new System.Drawing.Size(205, 6);
            // 
            // MenuItemFileClose
            // 
            MenuItemFileClose.Name = "MenuItemFileClose";
            MenuItemFileClose.Size = new System.Drawing.Size(208, 22);
            MenuItemFileClose.Text = "&Close";
            // 
            // MenuItemHelp
            // 
            MenuItemHelp.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { MenuItemHelpManual, MenuItemHelpPortForwarding, MenuItemHelpBugReport, MenuItemHelpSeparator1, MenuItemHelpUpdates, MenuItemHelpDiscord, MenuItemHelpAbout });
            MenuItemHelp.Name = "MenuItemHelp";
            MenuItemHelp.Size = new System.Drawing.Size(44, 20);
            MenuItemHelp.Text = "&Help";
            // 
            // MenuItemHelpManual
            // 
            MenuItemHelpManual.Image = Properties.Resources.OpenWeb_16x;
            MenuItemHelpManual.Name = "MenuItemHelpManual";
            MenuItemHelpManual.Size = new System.Drawing.Size(192, 22);
            MenuItemHelpManual.Text = "Online &Manual";
            // 
            // MenuItemHelpPortForwarding
            // 
            MenuItemHelpPortForwarding.Image = Properties.Resources.OpenWeb_16x;
            MenuItemHelpPortForwarding.Name = "MenuItemHelpPortForwarding";
            MenuItemHelpPortForwarding.Size = new System.Drawing.Size(192, 22);
            MenuItemHelpPortForwarding.Text = "&Port Forwarding";
            // 
            // MenuItemHelpBugReport
            // 
            MenuItemHelpBugReport.Image = Properties.Resources.NewBug_16x;
            MenuItemHelpBugReport.Name = "MenuItemHelpBugReport";
            MenuItemHelpBugReport.Size = new System.Drawing.Size(192, 22);
            MenuItemHelpBugReport.Text = "Submit a &Bug Report...";
            // 
            // MenuItemHelpSeparator1
            // 
            MenuItemHelpSeparator1.Name = "MenuItemHelpSeparator1";
            MenuItemHelpSeparator1.Size = new System.Drawing.Size(189, 6);
            // 
            // MenuItemHelpUpdates
            // 
            MenuItemHelpUpdates.Image = Properties.Resources.UnsyncedCommits_16x_Horiz;
            MenuItemHelpUpdates.Name = "MenuItemHelpUpdates";
            MenuItemHelpUpdates.Size = new System.Drawing.Size(192, 22);
            MenuItemHelpUpdates.Text = "Check for &Updates";
            // 
            // MenuItemHelpDiscord
            // 
            MenuItemHelpDiscord.Image = Properties.Resources.DiscordLogo;
            MenuItemHelpDiscord.Name = "MenuItemHelpDiscord";
            MenuItemHelpDiscord.Size = new System.Drawing.Size(192, 22);
            MenuItemHelpDiscord.Text = "Get support in &Discord";
            // 
            // MenuItemHelpAbout
            // 
            MenuItemHelpAbout.Name = "MenuItemHelpAbout";
            MenuItemHelpAbout.Size = new System.Drawing.Size(192, 22);
            MenuItemHelpAbout.Text = "&About...";
            // 
            // StatusStrip
            // 
            StatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { StatusStripLabelLeft, StatusStripLabelSpacer, StatusStripLabelRight });
            StatusStrip.Location = new System.Drawing.Point(0, 310);
            StatusStrip.Name = "StatusStrip";
            StatusStrip.Size = new System.Drawing.Size(484, 22);
            StatusStrip.TabIndex = 2;
            // 
            // StatusStripLabelLeft
            // 
            StatusStripLabelLeft.Name = "StatusStripLabelLeft";
            StatusStripLabelLeft.Size = new System.Drawing.Size(0, 17);
            // 
            // StatusStripLabelSpacer
            // 
            StatusStripLabelSpacer.Name = "StatusStripLabelSpacer";
            StatusStripLabelSpacer.Size = new System.Drawing.Size(469, 17);
            StatusStripLabelSpacer.Spring = true;
            // 
            // StatusStripLabelRight
            // 
            StatusStripLabelRight.Name = "StatusStripLabelRight";
            StatusStripLabelRight.Size = new System.Drawing.Size(0, 17);
            // 
            // Tabs
            // 
            Tabs.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            Tabs.Controls.Add(TabServerControls);
            Tabs.Controls.Add(TabAdvancedControls);
            Tabs.Controls.Add(TabServerDetails);
            Tabs.Controls.Add(TabPlayers);
            Tabs.Controls.Add(TabLogs);
            Tabs.Location = new System.Drawing.Point(12, 27);
            Tabs.Name = "Tabs";
            Tabs.SelectedIndex = 0;
            Tabs.Size = new System.Drawing.Size(460, 280);
            Tabs.TabIndex = 1;
            // 
            // TabServerControls
            // 
            TabServerControls.Controls.Add(CopyButtonServerPassword);
            TabServerControls.Controls.Add(JoinOptionsGroupBox);
            TabServerControls.Controls.Add(WorldSelectGroupBox);
            TabServerControls.Controls.Add(ServerPortField);
            TabServerControls.Controls.Add(ButtonRestartServer);
            TabServerControls.Controls.Add(ShowPasswordField);
            TabServerControls.Controls.Add(ServerPasswordField);
            TabServerControls.Controls.Add(ServerNameField);
            TabServerControls.Controls.Add(ButtonStopServer);
            TabServerControls.Controls.Add(ButtonStartServer);
            TabServerControls.Location = new System.Drawing.Point(4, 24);
            TabServerControls.Name = "TabServerControls";
            TabServerControls.Padding = new System.Windows.Forms.Padding(3);
            TabServerControls.Size = new System.Drawing.Size(452, 252);
            TabServerControls.TabIndex = 0;
            TabServerControls.Text = "Server Controls";
            TabServerControls.UseVisualStyleBackColor = true;
            // 
            // CopyButtonServerPassword
            // 
            CopyButtonServerPassword.CopyFunction = null;
            CopyButtonServerPassword.HelpText = "Copy password to clipboard";
            CopyButtonServerPassword.Location = new System.Drawing.Point(236, 69);
            CopyButtonServerPassword.Name = "CopyButtonServerPassword";
            CopyButtonServerPassword.Size = new System.Drawing.Size(16, 16);
            CopyButtonServerPassword.TabIndex = 3;
            CopyButtonServerPassword.TabStop = false;
            // 
            // JoinOptionsGroupBox
            // 
            JoinOptionsGroupBox.Controls.Add(CommunityServerField);
            JoinOptionsGroupBox.Controls.Add(ServerCrossplayField);
            JoinOptionsGroupBox.Location = new System.Drawing.Point(249, 94);
            JoinOptionsGroupBox.Name = "JoinOptionsGroupBox";
            JoinOptionsGroupBox.Size = new System.Drawing.Size(197, 96);
            JoinOptionsGroupBox.TabIndex = 6;
            JoinOptionsGroupBox.TabStop = false;
            JoinOptionsGroupBox.Text = "Join Options";
            // 
            // CommunityServerField
            // 
            CommunityServerField.HelpText = resources.GetString("CommunityServerField.HelpText");
            CommunityServerField.LabelText = "Community Server (Public)";
            CommunityServerField.Location = new System.Drawing.Point(6, 22);
            CommunityServerField.Name = "CommunityServerField";
            CommunityServerField.Size = new System.Drawing.Size(185, 17);
            CommunityServerField.TabIndex = 0;
            CommunityServerField.Value = false;
            // 
            // ServerCrossplayField
            // 
            ServerCrossplayField.HelpText = "Allow players on other platforms to join your game\r\n(Microsoft Store, Xbox, etc.)\r\n\r\nYou may need to provide other players an Invite Code,\r\nwhich appears in game and in the server logs.";
            ServerCrossplayField.LabelText = "Enable Crossplay";
            ServerCrossplayField.Location = new System.Drawing.Point(6, 45);
            ServerCrossplayField.Name = "ServerCrossplayField";
            ServerCrossplayField.Size = new System.Drawing.Size(133, 17);
            ServerCrossplayField.TabIndex = 1;
            ServerCrossplayField.Value = false;
            // 
            // WorldSelectGroupBox
            // 
            WorldSelectGroupBox.Controls.Add(WorldsListRefreshButton);
            WorldSelectGroupBox.Controls.Add(WorldsFolderOpenButton);
            WorldSelectGroupBox.Controls.Add(WorldSelectNewNameField);
            WorldSelectGroupBox.Controls.Add(WorldSelectRadioNew);
            WorldSelectGroupBox.Controls.Add(WorldSelectRadioExisting);
            WorldSelectGroupBox.Controls.Add(WorldSelectExistingNameField);
            WorldSelectGroupBox.Location = new System.Drawing.Point(3, 94);
            WorldSelectGroupBox.Name = "WorldSelectGroupBox";
            WorldSelectGroupBox.Size = new System.Drawing.Size(240, 96);
            WorldSelectGroupBox.TabIndex = 5;
            WorldSelectGroupBox.TabStop = false;
            WorldSelectGroupBox.Text = "World";
            // 
            // WorldsListRefreshButton
            // 
            WorldsListRefreshButton.HelpText = "Refresh the worlds list";
            WorldsListRefreshButton.Location = new System.Drawing.Point(188, 23);
            WorldsListRefreshButton.Name = "WorldsListRefreshButton";
            WorldsListRefreshButton.RefreshFunction = null;
            WorldsListRefreshButton.Size = new System.Drawing.Size(16, 16);
            WorldsListRefreshButton.TabIndex = 2;
            WorldsListRefreshButton.TabStop = false;
            // 
            // WorldsFolderOpenButton
            // 
            WorldsFolderOpenButton.HelpText = "Open the save data folder in Explorer";
            WorldsFolderOpenButton.Location = new System.Drawing.Point(210, 23);
            WorldsFolderOpenButton.Name = "WorldsFolderOpenButton";
            WorldsFolderOpenButton.PathFunction = null;
            WorldsFolderOpenButton.Size = new System.Drawing.Size(16, 16);
            WorldsFolderOpenButton.TabIndex = 3;
            WorldsFolderOpenButton.TabStop = false;
            // 
            // WorldSelectNewNameField
            // 
            WorldSelectNewNameField.HelpText = "";
            WorldSelectNewNameField.HideValue = false;
            WorldSelectNewNameField.LabelText = "New World Name";
            WorldSelectNewNameField.Location = new System.Drawing.Point(3, 45);
            WorldSelectNewNameField.MaxLength = 20;
            WorldSelectNewNameField.Multiline = false;
            WorldSelectNewNameField.Name = "WorldSelectNewNameField";
            WorldSelectNewNameField.Size = new System.Drawing.Size(234, 41);
            WorldSelectNewNameField.TabIndex = 5;
            WorldSelectNewNameField.Value = "";
            WorldSelectNewNameField.Visible = false;
            // 
            // WorldSelectRadioNew
            // 
            WorldSelectRadioNew.GroupName = "WorldSelect";
            WorldSelectRadioNew.HelpText = "";
            WorldSelectRadioNew.LabelText = "New";
            WorldSelectRadioNew.Location = new System.Drawing.Point(82, 23);
            WorldSelectRadioNew.Name = "WorldSelectRadioNew";
            WorldSelectRadioNew.Size = new System.Drawing.Size(65, 17);
            WorldSelectRadioNew.TabIndex = 1;
            WorldSelectRadioNew.Value = false;
            // 
            // WorldSelectRadioExisting
            // 
            WorldSelectRadioExisting.GroupName = "WorldSelect";
            WorldSelectRadioExisting.HelpText = "";
            WorldSelectRadioExisting.LabelText = "Existing";
            WorldSelectRadioExisting.Location = new System.Drawing.Point(6, 22);
            WorldSelectRadioExisting.Name = "WorldSelectRadioExisting";
            WorldSelectRadioExisting.Size = new System.Drawing.Size(83, 17);
            WorldSelectRadioExisting.TabIndex = 0;
            WorldSelectRadioExisting.Value = false;
            // 
            // WorldSelectExistingNameField
            // 
            WorldSelectExistingNameField.DataSource = (System.Collections.Generic.IEnumerable<string>)resources.GetObject("WorldSelectExistingNameField.DataSource");
            WorldSelectExistingNameField.DropdownEnabled = true;
            WorldSelectExistingNameField.EmptyText = "(no worlds)";
            WorldSelectExistingNameField.HelpText = "";
            WorldSelectExistingNameField.LabelText = "Select World";
            WorldSelectExistingNameField.Location = new System.Drawing.Point(6, 45);
            WorldSelectExistingNameField.Name = "WorldSelectExistingNameField";
            WorldSelectExistingNameField.Size = new System.Drawing.Size(234, 41);
            WorldSelectExistingNameField.TabIndex = 4;
            WorldSelectExistingNameField.Value = null;
            WorldSelectExistingNameField.Visible = false;
            // 
            // ServerPortField
            // 
            ServerPortField.HelpText = "";
            ServerPortField.LabelText = "Port";
            ServerPortField.Location = new System.Drawing.Point(236, 0);
            ServerPortField.Maximum = 65535;
            ServerPortField.Minimum = 1;
            ServerPortField.Name = "ServerPortField";
            ServerPortField.Size = new System.Drawing.Size(75, 41);
            ServerPortField.TabIndex = 1;
            ServerPortField.Value = 1;
            // 
            // ButtonRestartServer
            // 
            ButtonRestartServer.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            ButtonRestartServer.Image = Properties.Resources.Restart_16x;
            ButtonRestartServer.Location = new System.Drawing.Point(115, 226);
            ButtonRestartServer.Name = "ButtonRestartServer";
            ButtonRestartServer.Size = new System.Drawing.Size(106, 23);
            ButtonRestartServer.TabIndex = 8;
            ButtonRestartServer.Text = "Restart Server";
            ButtonRestartServer.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            ButtonRestartServer.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            ButtonRestartServer.UseVisualStyleBackColor = true;
            // 
            // ShowPasswordField
            // 
            ShowPasswordField.HelpText = "";
            ShowPasswordField.LabelText = "Show Password";
            ShowPasswordField.Location = new System.Drawing.Point(255, 68);
            ShowPasswordField.Name = "ShowPasswordField";
            ShowPasswordField.Size = new System.Drawing.Size(150, 17);
            ShowPasswordField.TabIndex = 4;
            ShowPasswordField.Value = false;
            // 
            // ServerPasswordField
            // 
            ServerPasswordField.HelpText = "Your server must be protected with a password. The password must be at least 5 characters \r\nand must not contain the name of the server or the world that you're hosting.";
            ServerPasswordField.HideValue = true;
            ServerPasswordField.LabelText = "Server Password";
            ServerPasswordField.Location = new System.Drawing.Point(0, 47);
            ServerPasswordField.MaxLength = 64;
            ServerPasswordField.Multiline = false;
            ServerPasswordField.Name = "ServerPasswordField";
            ServerPasswordField.Size = new System.Drawing.Size(243, 41);
            ServerPasswordField.TabIndex = 2;
            ServerPasswordField.Value = "";
            // 
            // ServerNameField
            // 
            ServerNameField.HelpText = "This is the name that will appear in the Community Servers list within Valheim.";
            ServerNameField.HideValue = false;
            ServerNameField.LabelText = "Server Name";
            ServerNameField.Location = new System.Drawing.Point(0, 0);
            ServerNameField.MaxLength = 64;
            ServerNameField.Multiline = false;
            ServerNameField.Name = "ServerNameField";
            ServerNameField.Size = new System.Drawing.Size(243, 41);
            ServerNameField.TabIndex = 0;
            ServerNameField.Value = "";
            // 
            // ButtonStopServer
            // 
            ButtonStopServer.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            ButtonStopServer.Image = Properties.Resources.Stop_16x;
            ButtonStopServer.Location = new System.Drawing.Point(227, 226);
            ButtonStopServer.Name = "ButtonStopServer";
            ButtonStopServer.Size = new System.Drawing.Size(106, 23);
            ButtonStopServer.TabIndex = 9;
            ButtonStopServer.Text = "Stop Server";
            ButtonStopServer.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            ButtonStopServer.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            ButtonStopServer.UseVisualStyleBackColor = true;
            // 
            // ButtonStartServer
            // 
            ButtonStartServer.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            ButtonStartServer.Image = Properties.Resources.Run_16x;
            ButtonStartServer.Location = new System.Drawing.Point(3, 226);
            ButtonStartServer.Name = "ButtonStartServer";
            ButtonStartServer.Size = new System.Drawing.Size(106, 23);
            ButtonStartServer.TabIndex = 7;
            ButtonStartServer.Text = "Start Server";
            ButtonStartServer.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            ButtonStartServer.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            ButtonStartServer.UseVisualStyleBackColor = true;
            // 
            // TabAdvancedControls
            // 
            TabAdvancedControls.Controls.Add(SavingGroupBox);
            TabAdvancedControls.Controls.Add(OtherSettingsGroupBox);
            TabAdvancedControls.Controls.Add(DirectoriesGroupBox);
            TabAdvancedControls.Location = new System.Drawing.Point(4, 24);
            TabAdvancedControls.Name = "TabAdvancedControls";
            TabAdvancedControls.Padding = new System.Windows.Forms.Padding(3);
            TabAdvancedControls.Size = new System.Drawing.Size(452, 252);
            TabAdvancedControls.TabIndex = 5;
            TabAdvancedControls.Text = "Advanced Controls";
            TabAdvancedControls.UseVisualStyleBackColor = true;
            // 
            // SavingGroupBox
            // 
            SavingGroupBox.Controls.Add(ServerLongBackupIntervalField);
            SavingGroupBox.Controls.Add(ServerSaveIntervalField);
            SavingGroupBox.Controls.Add(ServerShortBackupIntervalField);
            SavingGroupBox.Controls.Add(ServerBackupsField);
            SavingGroupBox.Location = new System.Drawing.Point(319, 6);
            SavingGroupBox.Name = "SavingGroupBox";
            SavingGroupBox.Size = new System.Drawing.Size(127, 240);
            SavingGroupBox.TabIndex = 2;
            SavingGroupBox.TabStop = false;
            SavingGroupBox.Text = "Saving && Backups";
            // 
            // ServerLongBackupIntervalField
            // 
            ServerLongBackupIntervalField.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            ServerLongBackupIntervalField.HelpText = "How often to create additional backups of the world\r\nsave data, in seconds. This interval must be longer than\r\nthe short backup interval.";
            ServerLongBackupIntervalField.LabelText = "Long Backup";
            ServerLongBackupIntervalField.Location = new System.Drawing.Point(6, 163);
            ServerLongBackupIntervalField.Maximum = 2592000;
            ServerLongBackupIntervalField.Minimum = 300;
            ServerLongBackupIntervalField.Name = "ServerLongBackupIntervalField";
            ServerLongBackupIntervalField.Size = new System.Drawing.Size(115, 41);
            ServerLongBackupIntervalField.TabIndex = 3;
            ServerLongBackupIntervalField.Value = 300;
            // 
            // ServerSaveIntervalField
            // 
            ServerSaveIntervalField.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            ServerSaveIntervalField.HelpText = "How often the world is saved, in seconds.";
            ServerSaveIntervalField.LabelText = "Save Interval";
            ServerSaveIntervalField.Location = new System.Drawing.Point(6, 22);
            ServerSaveIntervalField.Maximum = 86400;
            ServerSaveIntervalField.Minimum = 60;
            ServerSaveIntervalField.Name = "ServerSaveIntervalField";
            ServerSaveIntervalField.Size = new System.Drawing.Size(115, 41);
            ServerSaveIntervalField.TabIndex = 0;
            ServerSaveIntervalField.Value = 60;
            // 
            // ServerShortBackupIntervalField
            // 
            ServerShortBackupIntervalField.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            ServerShortBackupIntervalField.HelpText = "How often to create a rolling backup of the world\r\nsave data, in seconds.";
            ServerShortBackupIntervalField.LabelText = "Short Backup";
            ServerShortBackupIntervalField.Location = new System.Drawing.Point(6, 116);
            ServerShortBackupIntervalField.Maximum = 2592000;
            ServerShortBackupIntervalField.Minimum = 300;
            ServerShortBackupIntervalField.Name = "ServerShortBackupIntervalField";
            ServerShortBackupIntervalField.Size = new System.Drawing.Size(115, 41);
            ServerShortBackupIntervalField.TabIndex = 2;
            ServerShortBackupIntervalField.Value = 300;
            // 
            // ServerBackupsField
            // 
            ServerBackupsField.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            ServerBackupsField.HelpText = "Number of world data backups to maintain. One rolling backup\r\nis created on the short backup interval, and subsequent backups are\r\ncreated on the long backup interval.";
            ServerBackupsField.LabelText = "Backups";
            ServerBackupsField.Location = new System.Drawing.Point(6, 69);
            ServerBackupsField.Maximum = 1000;
            ServerBackupsField.Minimum = 1;
            ServerBackupsField.Name = "ServerBackupsField";
            ServerBackupsField.Size = new System.Drawing.Size(115, 41);
            ServerBackupsField.TabIndex = 1;
            ServerBackupsField.Value = 1;
            // 
            // OtherSettingsGroupBox
            // 
            OtherSettingsGroupBox.Controls.Add(ServerLogFileField);
            OtherSettingsGroupBox.Controls.Add(ServerAutoStartField);
            OtherSettingsGroupBox.Controls.Add(ServerAdditionalArgsField);
            OtherSettingsGroupBox.Location = new System.Drawing.Point(6, 131);
            OtherSettingsGroupBox.Name = "OtherSettingsGroupBox";
            OtherSettingsGroupBox.Size = new System.Drawing.Size(307, 115);
            OtherSettingsGroupBox.TabIndex = 1;
            OtherSettingsGroupBox.TabStop = false;
            OtherSettingsGroupBox.Text = "Other Settings";
            // 
            // ServerLogFileField
            // 
            ServerLogFileField.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            ServerLogFileField.HelpText = "If enabled, this server's logs will be written to a daily rolling\r\nlog file on disk. Logs older than 30 days will be deleted\r\nautomatically.\r\n";
            ServerLogFileField.LabelText = "Write server logs to file";
            ServerLogFileField.Location = new System.Drawing.Point(6, 45);
            ServerLogFileField.Name = "ServerLogFileField";
            ServerLogFileField.Size = new System.Drawing.Size(286, 17);
            ServerLogFileField.TabIndex = 1;
            ServerLogFileField.Value = false;
            // 
            // ServerAutoStartField
            // 
            ServerAutoStartField.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            ServerAutoStartField.HelpText = resources.GetString("ServerAutoStartField.HelpText");
            ServerAutoStartField.LabelText = "Start this server when ValheimServerGUI starts";
            ServerAutoStartField.Location = new System.Drawing.Point(6, 22);
            ServerAutoStartField.Name = "ServerAutoStartField";
            ServerAutoStartField.Size = new System.Drawing.Size(286, 17);
            ServerAutoStartField.TabIndex = 0;
            ServerAutoStartField.Value = false;
            // 
            // ServerAdditionalArgsField
            // 
            ServerAdditionalArgsField.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            ServerAdditionalArgsField.HelpText = "Add any additional args you want to pass to the server run\r\ncommand here. These will be appended to the end of the\r\ncommand generated by ValheimServerGUI.";
            ServerAdditionalArgsField.HideValue = false;
            ServerAdditionalArgsField.LabelText = "Additional Command Line Args";
            ServerAdditionalArgsField.Location = new System.Drawing.Point(6, 68);
            ServerAdditionalArgsField.MaxLength = 32767;
            ServerAdditionalArgsField.Multiline = false;
            ServerAdditionalArgsField.Name = "ServerAdditionalArgsField";
            ServerAdditionalArgsField.Size = new System.Drawing.Size(295, 41);
            ServerAdditionalArgsField.TabIndex = 2;
            ServerAdditionalArgsField.Value = "";
            // 
            // DirectoriesGroupBox
            // 
            DirectoriesGroupBox.Controls.Add(ServerSaveDataPathOpenButton);
            DirectoriesGroupBox.Controls.Add(ServerExePathOpenButton);
            DirectoriesGroupBox.Controls.Add(ServerSaveDataFolderPathField);
            DirectoriesGroupBox.Controls.Add(ServerExePathField);
            DirectoriesGroupBox.Location = new System.Drawing.Point(6, 6);
            DirectoriesGroupBox.Name = "DirectoriesGroupBox";
            DirectoriesGroupBox.Size = new System.Drawing.Size(307, 119);
            DirectoriesGroupBox.TabIndex = 0;
            DirectoriesGroupBox.TabStop = false;
            DirectoriesGroupBox.Text = "Directory Overrides (for just this Profile)";
            // 
            // ServerSaveDataPathOpenButton
            // 
            ServerSaveDataPathOpenButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            ServerSaveDataPathOpenButton.HelpText = "Open this folder in Explorer";
            ServerSaveDataPathOpenButton.Location = new System.Drawing.Point(285, 90);
            ServerSaveDataPathOpenButton.Name = "ServerSaveDataPathOpenButton";
            ServerSaveDataPathOpenButton.PathFunction = null;
            ServerSaveDataPathOpenButton.Size = new System.Drawing.Size(16, 16);
            ServerSaveDataPathOpenButton.TabIndex = 3;
            ServerSaveDataPathOpenButton.TabStop = false;
            // 
            // ServerExePathOpenButton
            // 
            ServerExePathOpenButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            ServerExePathOpenButton.HelpText = "Open this folder in Explorer";
            ServerExePathOpenButton.Location = new System.Drawing.Point(285, 43);
            ServerExePathOpenButton.Name = "ServerExePathOpenButton";
            ServerExePathOpenButton.PathFunction = null;
            ServerExePathOpenButton.Size = new System.Drawing.Size(16, 16);
            ServerExePathOpenButton.TabIndex = 1;
            ServerExePathOpenButton.TabStop = false;
            // 
            // ServerSaveDataFolderPathField
            // 
            ServerSaveDataFolderPathField.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            ServerSaveDataFolderPathField.FileSelectMode = ValheimServerGUI.Controls.FileSelectMode.Directory;
            ServerSaveDataFolderPathField.HelpText = resources.GetString("ServerSaveDataFolderPathField.HelpText");
            ServerSaveDataFolderPathField.InitialPath = null;
            ServerSaveDataFolderPathField.LabelText = "Valheim Save Data Folder";
            ServerSaveDataFolderPathField.Location = new System.Drawing.Point(6, 69);
            ServerSaveDataFolderPathField.MultiFileSeparator = "; ";
            ServerSaveDataFolderPathField.Name = "ServerSaveDataFolderPathField";
            ServerSaveDataFolderPathField.ReadOnly = false;
            ServerSaveDataFolderPathField.Size = new System.Drawing.Size(286, 41);
            ServerSaveDataFolderPathField.TabIndex = 2;
            ServerSaveDataFolderPathField.Value = "";
            // 
            // ServerExePathField
            // 
            ServerExePathField.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            ServerExePathField.FileSelectMode = ValheimServerGUI.Controls.FileSelectMode.SingleFile;
            ServerExePathField.HelpText = resources.GetString("ServerExePathField.HelpText");
            ServerExePathField.InitialPath = null;
            ServerExePathField.LabelText = "Valheim Dedicated Server .exe";
            ServerExePathField.Location = new System.Drawing.Point(6, 22);
            ServerExePathField.MultiFileSeparator = "; ";
            ServerExePathField.Name = "ServerExePathField";
            ServerExePathField.ReadOnly = false;
            ServerExePathField.Size = new System.Drawing.Size(286, 41);
            ServerExePathField.TabIndex = 0;
            ServerExePathField.Value = "";
            // 
            // TabServerDetails
            // 
            TabServerDetails.Controls.Add(groupBox2);
            TabServerDetails.Controls.Add(groupBox1);
            TabServerDetails.Location = new System.Drawing.Point(4, 24);
            TabServerDetails.Name = "TabServerDetails";
            TabServerDetails.Size = new System.Drawing.Size(452, 252);
            TabServerDetails.TabIndex = 4;
            TabServerDetails.Text = "Server Details";
            TabServerDetails.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(LabelSessionDuration);
            groupBox2.Controls.Add(LabelAverageWorldSave);
            groupBox2.Controls.Add(LabelLastWorldSave);
            groupBox2.Location = new System.Drawing.Point(3, 143);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new System.Drawing.Size(307, 100);
            groupBox2.TabIndex = 1;
            groupBox2.TabStop = false;
            groupBox2.Text = "Statistics";
            // 
            // LabelSessionDuration
            // 
            LabelSessionDuration.HelpText = "";
            LabelSessionDuration.LabelSplitRatio = 0.5D;
            LabelSessionDuration.LabelText = "Server Uptime:";
            LabelSessionDuration.LabelTextAlign = System.Drawing.ContentAlignment.TopRight;
            LabelSessionDuration.Location = new System.Drawing.Point(7, 23);
            LabelSessionDuration.Name = "LabelSessionDuration";
            LabelSessionDuration.Size = new System.Drawing.Size(241, 15);
            LabelSessionDuration.TabIndex = 0;
            LabelSessionDuration.TabStop = false;
            LabelSessionDuration.Value = "";
            LabelSessionDuration.ValueTextAlign = System.Drawing.ContentAlignment.TopLeft;
            // 
            // LabelAverageWorldSave
            // 
            LabelAverageWorldSave.HelpText = "The average amount of time it has taken to save\r\nyour game world, over the past 10 saves.";
            LabelAverageWorldSave.LabelSplitRatio = 0.455D;
            LabelAverageWorldSave.LabelText = "Avg. World Save:";
            LabelAverageWorldSave.LabelTextAlign = System.Drawing.ContentAlignment.TopRight;
            LabelAverageWorldSave.Location = new System.Drawing.Point(6, 65);
            LabelAverageWorldSave.Name = "LabelAverageWorldSave";
            LabelAverageWorldSave.Size = new System.Drawing.Size(264, 15);
            LabelAverageWorldSave.TabIndex = 2;
            LabelAverageWorldSave.TabStop = false;
            LabelAverageWorldSave.Value = "";
            LabelAverageWorldSave.ValueTextAlign = System.Drawing.ContentAlignment.TopLeft;
            // 
            // LabelLastWorldSave
            // 
            LabelLastWorldSave.HelpText = "";
            LabelLastWorldSave.LabelSplitRatio = 0.405D;
            LabelLastWorldSave.LabelText = "Last World Save:";
            LabelLastWorldSave.LabelTextAlign = System.Drawing.ContentAlignment.TopRight;
            LabelLastWorldSave.Location = new System.Drawing.Point(6, 44);
            LabelLastWorldSave.Name = "LabelLastWorldSave";
            LabelLastWorldSave.Size = new System.Drawing.Size(295, 15);
            LabelLastWorldSave.TabIndex = 1;
            LabelLastWorldSave.TabStop = false;
            LabelLastWorldSave.Value = "";
            LabelLastWorldSave.ValueTextAlign = System.Drawing.ContentAlignment.TopLeft;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(CopyButtonInviteCode);
            groupBox1.Controls.Add(LabelInviteCode);
            groupBox1.Controls.Add(CopyButtonLocalIpAddress);
            groupBox1.Controls.Add(CopyButtonExternalIpAddress);
            groupBox1.Controls.Add(CopyButtonInternalIpAddress);
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(LabelExternalIpAddress);
            groupBox1.Controls.Add(LabelLocalIpAddress);
            groupBox1.Controls.Add(LabelInternalIpAddress);
            groupBox1.Location = new System.Drawing.Point(3, 3);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new System.Drawing.Size(307, 134);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "Connection Details";
            // 
            // CopyButtonInviteCode
            // 
            CopyButtonInviteCode.CopyFunction = null;
            CopyButtonInviteCode.HelpText = "Copy invite code to clipboard";
            CopyButtonInviteCode.Location = new System.Drawing.Point(276, 85);
            CopyButtonInviteCode.Name = "CopyButtonInviteCode";
            CopyButtonInviteCode.Size = new System.Drawing.Size(16, 16);
            CopyButtonInviteCode.TabIndex = 7;
            CopyButtonInviteCode.TabStop = false;
            // 
            // LabelInviteCode
            // 
            LabelInviteCode.HelpText = resources.GetString("LabelInviteCode.HelpText");
            LabelInviteCode.LabelSplitRatio = 0.455D;
            LabelInviteCode.LabelText = "Invite Code:";
            LabelInviteCode.LabelTextAlign = System.Drawing.ContentAlignment.TopRight;
            LabelInviteCode.Location = new System.Drawing.Point(6, 85);
            LabelInviteCode.Name = "LabelInviteCode";
            LabelInviteCode.Size = new System.Drawing.Size(264, 15);
            LabelInviteCode.TabIndex = 6;
            LabelInviteCode.TabStop = false;
            LabelInviteCode.Value = "N/A";
            LabelInviteCode.ValueTextAlign = System.Drawing.ContentAlignment.TopLeft;
            // 
            // CopyButtonLocalIpAddress
            // 
            CopyButtonLocalIpAddress.CopyFunction = null;
            CopyButtonLocalIpAddress.HelpText = "Copy local IP address to clipboard";
            CopyButtonLocalIpAddress.Location = new System.Drawing.Point(276, 63);
            CopyButtonLocalIpAddress.Name = "CopyButtonLocalIpAddress";
            CopyButtonLocalIpAddress.Size = new System.Drawing.Size(16, 16);
            CopyButtonLocalIpAddress.TabIndex = 5;
            CopyButtonLocalIpAddress.TabStop = false;
            // 
            // CopyButtonExternalIpAddress
            // 
            CopyButtonExternalIpAddress.CopyFunction = null;
            CopyButtonExternalIpAddress.HelpText = "Copy external IP address to clipboard";
            CopyButtonExternalIpAddress.Location = new System.Drawing.Point(276, 22);
            CopyButtonExternalIpAddress.Name = "CopyButtonExternalIpAddress";
            CopyButtonExternalIpAddress.Size = new System.Drawing.Size(16, 16);
            CopyButtonExternalIpAddress.TabIndex = 1;
            CopyButtonExternalIpAddress.TabStop = false;
            // 
            // CopyButtonInternalIpAddress
            // 
            CopyButtonInternalIpAddress.CopyFunction = null;
            CopyButtonInternalIpAddress.HelpText = "Copy internal IP address to clipboard";
            CopyButtonInternalIpAddress.Location = new System.Drawing.Point(276, 43);
            CopyButtonInternalIpAddress.Name = "CopyButtonInternalIpAddress";
            CopyButtonInternalIpAddress.Size = new System.Drawing.Size(16, 16);
            CopyButtonInternalIpAddress.TabIndex = 3;
            CopyButtonInternalIpAddress.TabStop = false;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point);
            label1.Location = new System.Drawing.Point(6, 110);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(261, 15);
            label1.TabIndex = 8;
            label1.Text = "Trouble connecting? See Help > Port Forwarding.\r\n";
            // 
            // LabelExternalIpAddress
            // 
            LabelExternalIpAddress.HelpText = "This is the address that players from outside your home network will use to\r\nconnect to your server. Give this address to your friends for standard online play.\r\n";
            LabelExternalIpAddress.LabelSplitRatio = 0.455D;
            LabelExternalIpAddress.LabelText = "External IP Address:";
            LabelExternalIpAddress.LabelTextAlign = System.Drawing.ContentAlignment.TopRight;
            LabelExternalIpAddress.Location = new System.Drawing.Point(6, 22);
            LabelExternalIpAddress.Name = "LabelExternalIpAddress";
            LabelExternalIpAddress.Size = new System.Drawing.Size(264, 15);
            LabelExternalIpAddress.TabIndex = 0;
            LabelExternalIpAddress.TabStop = false;
            LabelExternalIpAddress.Value = "Loading...";
            LabelExternalIpAddress.ValueTextAlign = System.Drawing.ContentAlignment.TopLeft;
            // 
            // LabelLocalIpAddress
            // 
            LabelLocalIpAddress.HelpText = resources.GetString("LabelLocalIpAddress.HelpText");
            LabelLocalIpAddress.LabelSplitRatio = 0.455D;
            LabelLocalIpAddress.LabelText = "Local IP Address:";
            LabelLocalIpAddress.LabelTextAlign = System.Drawing.ContentAlignment.TopRight;
            LabelLocalIpAddress.Location = new System.Drawing.Point(6, 64);
            LabelLocalIpAddress.Name = "LabelLocalIpAddress";
            LabelLocalIpAddress.Size = new System.Drawing.Size(264, 15);
            LabelLocalIpAddress.TabIndex = 4;
            LabelLocalIpAddress.TabStop = false;
            LabelLocalIpAddress.Value = "127.0.0.1";
            LabelLocalIpAddress.ValueTextAlign = System.Drawing.ContentAlignment.TopLeft;
            // 
            // LabelInternalIpAddress
            // 
            LabelInternalIpAddress.HelpText = resources.GetString("LabelInternalIpAddress.HelpText");
            LabelInternalIpAddress.LabelSplitRatio = 0.455D;
            LabelInternalIpAddress.LabelText = "Internal IP Address:";
            LabelInternalIpAddress.LabelTextAlign = System.Drawing.ContentAlignment.TopRight;
            LabelInternalIpAddress.Location = new System.Drawing.Point(6, 43);
            LabelInternalIpAddress.Name = "LabelInternalIpAddress";
            LabelInternalIpAddress.Size = new System.Drawing.Size(264, 15);
            LabelInternalIpAddress.TabIndex = 2;
            LabelInternalIpAddress.TabStop = false;
            LabelInternalIpAddress.Value = "Loading...";
            LabelInternalIpAddress.ValueTextAlign = System.Drawing.ContentAlignment.TopLeft;
            // 
            // TabPlayers
            // 
            TabPlayers.Controls.Add(ButtonRemovePlayer);
            TabPlayers.Controls.Add(ButtonPlayerDetails);
            TabPlayers.Controls.Add(PlayersTable);
            TabPlayers.Location = new System.Drawing.Point(4, 24);
            TabPlayers.Name = "TabPlayers";
            TabPlayers.Size = new System.Drawing.Size(452, 252);
            TabPlayers.TabIndex = 3;
            TabPlayers.Text = "Players";
            TabPlayers.UseVisualStyleBackColor = true;
            // 
            // ButtonRemovePlayer
            // 
            ButtonRemovePlayer.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            ButtonRemovePlayer.Enabled = false;
            ButtonRemovePlayer.Image = Properties.Resources.Cancel_16x;
            ButtonRemovePlayer.Location = new System.Drawing.Point(426, 3);
            ButtonRemovePlayer.Name = "ButtonRemovePlayer";
            ButtonRemovePlayer.Size = new System.Drawing.Size(23, 23);
            ButtonRemovePlayer.TabIndex = 1;
            ButtonRemovePlayer.UseVisualStyleBackColor = true;
            // 
            // ButtonPlayerDetails
            // 
            ButtonPlayerDetails.Enabled = false;
            ButtonPlayerDetails.Location = new System.Drawing.Point(3, 3);
            ButtonPlayerDetails.Name = "ButtonPlayerDetails";
            ButtonPlayerDetails.Size = new System.Drawing.Size(132, 23);
            ButtonPlayerDetails.TabIndex = 0;
            ButtonPlayerDetails.Text = "View Player Details...";
            ButtonPlayerDetails.UseVisualStyleBackColor = true;
            // 
            // PlayersTable
            // 
            PlayersTable.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            PlayersTable.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { ColumnPlayerName, ColumnPlayerStatus, ColumnPlayerUpdated });
            PlayersTable.Icons = ImageList;
            PlayersTable.Location = new System.Drawing.Point(3, 32);
            PlayersTable.Name = "PlayersTable";
            PlayersTable.Size = new System.Drawing.Size(446, 217);
            PlayersTable.TabIndex = 2;
            // 
            // ColumnPlayerName
            // 
            ColumnPlayerName.Text = "Player (Character)";
            ColumnPlayerName.Width = 240;
            // 
            // ColumnPlayerStatus
            // 
            ColumnPlayerStatus.Text = "Status";
            ColumnPlayerStatus.Width = 80;
            // 
            // ColumnPlayerUpdated
            // 
            ColumnPlayerUpdated.Text = "Since";
            ColumnPlayerUpdated.Width = 120;
            // 
            // ImageList
            // 
            ImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            ImageList.ImageSize = new System.Drawing.Size(16, 16);
            ImageList.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // TabLogs
            // 
            TabLogs.Controls.Add(LogsFolderOpenButton);
            TabLogs.Controls.Add(ButtonSaveLogs);
            TabLogs.Controls.Add(LogViewSelectField);
            TabLogs.Controls.Add(LogViewer);
            TabLogs.Controls.Add(ButtonClearLogs);
            TabLogs.Location = new System.Drawing.Point(4, 24);
            TabLogs.Name = "TabLogs";
            TabLogs.Size = new System.Drawing.Size(452, 252);
            TabLogs.TabIndex = 2;
            TabLogs.Text = "Logs";
            TabLogs.UseVisualStyleBackColor = true;
            // 
            // LogsFolderOpenButton
            // 
            LogsFolderOpenButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            LogsFolderOpenButton.HelpText = "Open the logs folder in Explorer";
            LogsFolderOpenButton.Location = new System.Drawing.Point(258, 25);
            LogsFolderOpenButton.Name = "LogsFolderOpenButton";
            LogsFolderOpenButton.PathFunction = null;
            LogsFolderOpenButton.Size = new System.Drawing.Size(16, 16);
            LogsFolderOpenButton.TabIndex = 1;
            // 
            // ButtonSaveLogs
            // 
            ButtonSaveLogs.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            ButtonSaveLogs.Location = new System.Drawing.Point(280, 22);
            ButtonSaveLogs.Name = "ButtonSaveLogs";
            ButtonSaveLogs.Size = new System.Drawing.Size(88, 23);
            ButtonSaveLogs.TabIndex = 2;
            ButtonSaveLogs.Text = "Save Logs...";
            ButtonSaveLogs.UseVisualStyleBackColor = true;
            // 
            // LogViewSelectField
            // 
            LogViewSelectField.DataSource = (System.Collections.Generic.IEnumerable<string>)resources.GetObject("LogViewSelectField.DataSource");
            LogViewSelectField.DropdownEnabled = true;
            LogViewSelectField.EmptyText = "";
            LogViewSelectField.HelpText = "";
            LogViewSelectField.LabelText = "View logs for...";
            LogViewSelectField.Location = new System.Drawing.Point(-4, 4);
            LogViewSelectField.Name = "LogViewSelectField";
            LogViewSelectField.Size = new System.Drawing.Size(150, 41);
            LogViewSelectField.TabIndex = 0;
            LogViewSelectField.Value = null;
            // 
            // LogViewer
            // 
            LogViewer.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            LogViewer.Location = new System.Drawing.Point(3, 51);
            LogViewer.LogView = "DefaultLogView";
            LogViewer.Name = "LogViewer";
            LogViewer.Size = new System.Drawing.Size(446, 198);
            LogViewer.TabIndex = 4;
            LogViewer.TabStop = false;
            // 
            // ButtonClearLogs
            // 
            ButtonClearLogs.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            ButtonClearLogs.Location = new System.Drawing.Point(374, 22);
            ButtonClearLogs.Name = "ButtonClearLogs";
            ButtonClearLogs.Size = new System.Drawing.Size(75, 23);
            ButtonClearLogs.TabIndex = 3;
            ButtonClearLogs.Text = "Clear Logs";
            ButtonClearLogs.UseVisualStyleBackColor = true;
            // 
            // NotifyIcon
            // 
            NotifyIcon.ContextMenuStrip = TrayContextMenuStrip;
            NotifyIcon.Icon = (System.Drawing.Icon)resources.GetObject("NotifyIcon.Icon");
            NotifyIcon.Text = "ValheimServerGUI";
            // 
            // TrayContextMenuStrip
            // 
            TrayContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { TrayContextMenuServerName, TrayContextMenuSeparator2, TrayContextMenuStart, TrayContextMenuRestart, TrayContextMenuStop, TrayContextMenuSeparator1, TrayContextMenuClose });
            TrayContextMenuStrip.Name = "TrayContextMenuStrip";
            TrayContextMenuStrip.Size = new System.Drawing.Size(146, 126);
            // 
            // TrayContextMenuServerName
            // 
            TrayContextMenuServerName.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            TrayContextMenuServerName.Name = "TrayContextMenuServerName";
            TrayContextMenuServerName.Size = new System.Drawing.Size(145, 22);
            TrayContextMenuServerName.Text = "ServerName";
            // 
            // TrayContextMenuSeparator2
            // 
            TrayContextMenuSeparator2.Name = "TrayContextMenuSeparator2";
            TrayContextMenuSeparator2.Size = new System.Drawing.Size(142, 6);
            // 
            // TrayContextMenuStart
            // 
            TrayContextMenuStart.Enabled = false;
            TrayContextMenuStart.Image = Properties.Resources.Run_16x;
            TrayContextMenuStart.Name = "TrayContextMenuStart";
            TrayContextMenuStart.Size = new System.Drawing.Size(145, 22);
            TrayContextMenuStart.Text = "Start Server";
            // 
            // TrayContextMenuRestart
            // 
            TrayContextMenuRestart.Enabled = false;
            TrayContextMenuRestart.Image = Properties.Resources.Restart_16x;
            TrayContextMenuRestart.Name = "TrayContextMenuRestart";
            TrayContextMenuRestart.Size = new System.Drawing.Size(145, 22);
            TrayContextMenuRestart.Text = "Restart Server";
            // 
            // TrayContextMenuStop
            // 
            TrayContextMenuStop.Enabled = false;
            TrayContextMenuStop.Image = Properties.Resources.Stop_16x;
            TrayContextMenuStop.Name = "TrayContextMenuStop";
            TrayContextMenuStop.Size = new System.Drawing.Size(145, 22);
            TrayContextMenuStop.Text = "Stop Server";
            // 
            // TrayContextMenuSeparator1
            // 
            TrayContextMenuSeparator1.Name = "TrayContextMenuSeparator1";
            TrayContextMenuSeparator1.Size = new System.Drawing.Size(142, 6);
            // 
            // TrayContextMenuClose
            // 
            TrayContextMenuClose.Name = "TrayContextMenuClose";
            TrayContextMenuClose.Size = new System.Drawing.Size(145, 22);
            TrayContextMenuClose.Text = "Close";
            // 
            // ServerRefreshTimer
            // 
            ServerRefreshTimer.Enabled = true;
            ServerRefreshTimer.Interval = 1000;
            // 
            // UpdateCheckTimer
            // 
            UpdateCheckTimer.Enabled = true;
            UpdateCheckTimer.Interval = 60000;
            // 
            // MainWindow
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(484, 332);
            Controls.Add(Tabs);
            Controls.Add(StatusStrip);
            Controls.Add(MenuStrip);
            MainMenuStrip = MenuStrip;
            MaximizeBox = false;
            MinimumSize = new System.Drawing.Size(500, 371);
            Name = "MainWindow";
            Text = "ApplicationTitle";
            MenuStrip.ResumeLayout(false);
            MenuStrip.PerformLayout();
            StatusStrip.ResumeLayout(false);
            StatusStrip.PerformLayout();
            Tabs.ResumeLayout(false);
            TabServerControls.ResumeLayout(false);
            JoinOptionsGroupBox.ResumeLayout(false);
            WorldSelectGroupBox.ResumeLayout(false);
            TabAdvancedControls.ResumeLayout(false);
            SavingGroupBox.ResumeLayout(false);
            OtherSettingsGroupBox.ResumeLayout(false);
            DirectoriesGroupBox.ResumeLayout(false);
            TabServerDetails.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            TabPlayers.ResumeLayout(false);
            TabLogs.ResumeLayout(false);
            TrayContextMenuStrip.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.MenuStrip MenuStrip;
        private System.Windows.Forms.ToolStripMenuItem MenuItemFile;
        private System.Windows.Forms.ToolStripSeparator MenuItemFileSeparator1;
        private System.Windows.Forms.ToolStripMenuItem MenuItemFileClose;
        private System.Windows.Forms.ToolStripMenuItem MenuItemHelp;
        private System.Windows.Forms.ToolStripMenuItem MenuItemHelpUpdates;
        private System.Windows.Forms.ToolStripSeparator MenuItemHelpSeparator1;
        private System.Windows.Forms.ToolStripMenuItem MenuItemHelpAbout;
        private System.Windows.Forms.StatusStrip StatusStrip;
        private System.Windows.Forms.ToolStripStatusLabel StatusStripLabelLeft;
        private System.Windows.Forms.TabControl Tabs;
        private System.Windows.Forms.TabPage TabServerControls;
        private System.Windows.Forms.TabPage TabLogs;
        private System.Windows.Forms.Button ButtonStopServer;
        private System.Windows.Forms.Button ButtonStartServer;
        private System.Windows.Forms.Button ButtonClearLogs;
        private ValheimServerGUI.Controls.DropdownFormField WorldSelectExistingNameField;
        private Controls.TextFormField ServerPasswordField;
        private Controls.TextFormField ServerNameField;
        private ValheimServerGUI.Controls.CheckboxFormField CommunityServerField;
        private ValheimServerGUI.Controls.CheckboxFormField ShowPasswordField;
        private System.Windows.Forms.Button ButtonRestartServer;
        private ValheimServerGUI.Controls.NumericFormField ServerPortField;
        private System.Windows.Forms.NotifyIcon NotifyIcon;
        private System.Windows.Forms.ContextMenuStrip TrayContextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem TrayContextMenuStart;
        private System.Windows.Forms.ToolStripMenuItem TrayContextMenuRestart;
        private System.Windows.Forms.ToolStripMenuItem TrayContextMenuStop;
        private System.Windows.Forms.ToolStripSeparator TrayContextMenuSeparator1;
        private System.Windows.Forms.ToolStripMenuItem TrayContextMenuClose;
        private System.Windows.Forms.ToolStripMenuItem MenuItemHelpManual;
        private System.Windows.Forms.GroupBox WorldSelectGroupBox;
        private ValheimServerGUI.Controls.RadioFormField WorldSelectRadioNew;
        private ValheimServerGUI.Controls.RadioFormField WorldSelectRadioExisting;
        private Controls.TextFormField WorldSelectNewNameField;
        private System.Windows.Forms.TabPage TabPlayers;
        private System.Windows.Forms.ImageList ImageList;
        private System.Windows.Forms.Timer ServerRefreshTimer;
        private ValheimServerGUI.Controls.DataListView PlayersTable;
        private System.Windows.Forms.ColumnHeader ColumnPlayerStatus;
        private System.Windows.Forms.ColumnHeader ColumnPlayerName;
        private System.Windows.Forms.ColumnHeader ColumnPlayerUpdated;
        private ValheimServerGUI.Controls.LogViewer LogViewer;
        private ValheimServerGUI.Controls.DropdownFormField LogViewSelectField;
        private System.Windows.Forms.Button ButtonSaveLogs;
        private System.Windows.Forms.Button ButtonPlayerDetails;
        private System.Windows.Forms.Button ButtonRemovePlayer;
        private System.Windows.Forms.TabPage TabServerDetails;
        private System.Windows.Forms.GroupBox groupBox1;
        private ValheimServerGUI.Controls.LabelField LabelLocalIpAddress;
        private ValheimServerGUI.Controls.LabelField LabelInternalIpAddress;
        private ValheimServerGUI.Controls.LabelField LabelExternalIpAddress;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ToolStripMenuItem MenuItemHelpPortForwarding;
        private CopyButton CopyButtonLocalIpAddress;
        private CopyButton CopyButtonExternalIpAddress;
        private CopyButton CopyButtonInternalIpAddress;
        private ValheimServerGUI.Controls.LabelField LabelAverageWorldSave;
        private ValheimServerGUI.Controls.LabelField LabelLastWorldSave;
        private ValheimServerGUI.Controls.LabelField LabelSessionDuration;
        private System.Windows.Forms.ToolStripStatusLabel StatusStripLabelSpacer;
        private System.Windows.Forms.ToolStripStatusLabel StatusStripLabelRight;
        private System.Windows.Forms.Timer UpdateCheckTimer;
        private System.Windows.Forms.ToolStripMenuItem MenuItemFilePreferences;
        private System.Windows.Forms.ToolStripMenuItem MenuItemHelpBugReport;
        private ValheimServerGUI.Controls.CheckboxFormField ServerCrossplayField;
        private System.Windows.Forms.ToolStripMenuItem TrayContextMenuServerName;
        private System.Windows.Forms.ToolStripSeparator TrayContextMenuSeparator2;
        private CopyButton CopyButtonInviteCode;
        private ValheimServerGUI.Controls.LabelField LabelInviteCode;
        private System.Windows.Forms.GroupBox JoinOptionsGroupBox;
        private CopyButton CopyButtonServerPassword;
        private System.Windows.Forms.ToolStripMenuItem MenuItemFileNewWindow;
        private System.Windows.Forms.ToolStripSeparator MenuItemFileSeparator2;
        private System.Windows.Forms.ToolStripMenuItem MenuItemFileSaveProfile;
        private System.Windows.Forms.ToolStripMenuItem MenuItemFileLoadProfile;
        private System.Windows.Forms.ToolStripMenuItem MenuItemFileRemoveProfile;
        private System.Windows.Forms.ToolStripSeparator MenuItemFileSeparator3;
        private System.Windows.Forms.ToolStripMenuItem MenuItemFileNewProfile;
        private System.Windows.Forms.TabPage TabAdvancedControls;
        private System.Windows.Forms.GroupBox DirectoriesGroupBox;
        private ValheimServerGUI.Controls.NumericFormField ServerLongBackupIntervalField;
        private ValheimServerGUI.Controls.NumericFormField ServerShortBackupIntervalField;
        private ValheimServerGUI.Controls.NumericFormField ServerBackupsField;
        private ValheimServerGUI.Controls.NumericFormField ServerSaveIntervalField;
        private Controls.TextFormField ServerAdditionalArgsField;
        private System.Windows.Forms.ToolStripMenuItem MenuItemFileSaveProfileAs;
        private System.Windows.Forms.ToolStripMenuItem MenuItemFileOpenSettings;
        private System.Windows.Forms.GroupBox OtherSettingsGroupBox;
        private ValheimServerGUI.Controls.CheckboxFormField ServerAutoStartField;
        private System.Windows.Forms.ToolStripMenuItem MenuItemHelpDiscord;
        private System.Windows.Forms.GroupBox SavingGroupBox;
        private ValheimServerGUI.Controls.FilenameFormField ServerSaveDataFolderPathField;
        private ValheimServerGUI.Controls.FilenameFormField ServerExePathField;
        private System.Windows.Forms.ToolStripMenuItem MenuItemFileDirectories;
        private OpenButton ServerSaveDataPathOpenButton;
        private OpenButton ServerExePathOpenButton;
        private OpenButton WorldsFolderOpenButton;
        private RefreshButton WorldsListRefreshButton;
        private ValheimServerGUI.Controls.CheckboxFormField ServerLogFileField;
        private OpenButton LogsFolderOpenButton;
    }
}