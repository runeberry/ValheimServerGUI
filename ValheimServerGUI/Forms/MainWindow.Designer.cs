
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
            this.MenuStrip = new System.Windows.Forms.MenuStrip();
            this.MenuItemFile = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemFileNewWindow = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemFileSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.MenuItemFileNewProfile = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemFileSaveProfile = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemFileSaveProfileAs = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemFileLoadProfile = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemFileRemoveProfile = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemFileSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.MenuItemFilePreferences = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemFileDirectories = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemFileOpenSettings = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemFileSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.MenuItemFileClose = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemHelp = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemHelpManual = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemHelpPortForwarding = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemHelpBugReport = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemHelpSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.MenuItemHelpUpdates = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemHelpAbout = new System.Windows.Forms.ToolStripMenuItem();
            this.StatusStrip = new System.Windows.Forms.StatusStrip();
            this.StatusStripLabelLeft = new System.Windows.Forms.ToolStripStatusLabel();
            this.StatusStripLabelSpacer = new System.Windows.Forms.ToolStripStatusLabel();
            this.StatusStripLabelRight = new System.Windows.Forms.ToolStripStatusLabel();
            this.Tabs = new System.Windows.Forms.TabControl();
            this.TabServerControls = new System.Windows.Forms.TabPage();
            this.CopyButtonServerPassword = new ValheimServerGUI.Forms.CopyButton();
            this.JoinOptionsGroupBox = new System.Windows.Forms.GroupBox();
            this.CommunityServerField = new ValheimServerGUI.Controls.CheckboxFormField();
            this.ServerCrossplayField = new ValheimServerGUI.Controls.CheckboxFormField();
            this.WorldSelectGroupBox = new System.Windows.Forms.GroupBox();
            this.WorldSelectNewNameField = new ValheimServerGUI.Forms.Controls.TextFormField();
            this.WorldSelectRadioNew = new ValheimServerGUI.Controls.RadioFormField();
            this.WorldSelectRadioExisting = new ValheimServerGUI.Controls.RadioFormField();
            this.WorldSelectExistingNameField = new ValheimServerGUI.Controls.DropdownFormField();
            this.ServerPortField = new ValheimServerGUI.Controls.NumericFormField();
            this.ButtonRestartServer = new System.Windows.Forms.Button();
            this.ShowPasswordField = new ValheimServerGUI.Controls.CheckboxFormField();
            this.ServerPasswordField = new ValheimServerGUI.Forms.Controls.TextFormField();
            this.ServerNameField = new ValheimServerGUI.Forms.Controls.TextFormField();
            this.ButtonStopServer = new System.Windows.Forms.Button();
            this.ButtonStartServer = new System.Windows.Forms.Button();
            this.TabAdvancedControls = new System.Windows.Forms.TabPage();
            this.ServerAdditionalArgsField = new ValheimServerGUI.Forms.Controls.TextFormField();
            this.SavingGroupBox = new System.Windows.Forms.GroupBox();
            this.ServerLongBackupIntervalField = new ValheimServerGUI.Controls.NumericFormField();
            this.ServerShortBackupIntervalField = new ValheimServerGUI.Controls.NumericFormField();
            this.ServerBackupsField = new ValheimServerGUI.Controls.NumericFormField();
            this.ServerSaveIntervalField = new ValheimServerGUI.Controls.NumericFormField();
            this.TabServerDetails = new System.Windows.Forms.TabPage();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.LabelSessionDuration = new ValheimServerGUI.Controls.LabelField();
            this.LabelAverageWorldSave = new ValheimServerGUI.Controls.LabelField();
            this.LabelLastWorldSave = new ValheimServerGUI.Controls.LabelField();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.CopyButtonInviteCode = new ValheimServerGUI.Forms.CopyButton();
            this.LabelInviteCode = new ValheimServerGUI.Controls.LabelField();
            this.CopyButtonLocalIpAddress = new ValheimServerGUI.Forms.CopyButton();
            this.CopyButtonExternalIpAddress = new ValheimServerGUI.Forms.CopyButton();
            this.CopyButtonInternalIpAddress = new ValheimServerGUI.Forms.CopyButton();
            this.label1 = new System.Windows.Forms.Label();
            this.LabelExternalIpAddress = new ValheimServerGUI.Controls.LabelField();
            this.LabelLocalIpAddress = new ValheimServerGUI.Controls.LabelField();
            this.LabelInternalIpAddress = new ValheimServerGUI.Controls.LabelField();
            this.TabPlayers = new System.Windows.Forms.TabPage();
            this.ButtonRemovePlayer = new System.Windows.Forms.Button();
            this.ButtonPlayerDetails = new System.Windows.Forms.Button();
            this.PlayersTable = new ValheimServerGUI.Controls.DataListView();
            this.ColumnPlayerStatus = new System.Windows.Forms.ColumnHeader();
            this.ColumnPlayerName = new System.Windows.Forms.ColumnHeader();
            this.ColumnPlayerUpdated = new System.Windows.Forms.ColumnHeader();
            this.ImageList = new System.Windows.Forms.ImageList(this.components);
            this.TabLogs = new System.Windows.Forms.TabPage();
            this.ButtonSaveLogs = new System.Windows.Forms.Button();
            this.LogViewSelectField = new ValheimServerGUI.Controls.DropdownFormField();
            this.LogViewer = new ValheimServerGUI.Controls.LogViewer();
            this.ButtonClearLogs = new System.Windows.Forms.Button();
            this.NotifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.TrayContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.TrayContextMenuServerName = new System.Windows.Forms.ToolStripMenuItem();
            this.TrayContextMenuSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.TrayContextMenuStart = new System.Windows.Forms.ToolStripMenuItem();
            this.TrayContextMenuRestart = new System.Windows.Forms.ToolStripMenuItem();
            this.TrayContextMenuStop = new System.Windows.Forms.ToolStripMenuItem();
            this.TrayContextMenuSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.TrayContextMenuClose = new System.Windows.Forms.ToolStripMenuItem();
            this.ServerRefreshTimer = new System.Windows.Forms.Timer(this.components);
            this.UpdateCheckTimer = new System.Windows.Forms.Timer(this.components);
            this.MenuStrip.SuspendLayout();
            this.StatusStrip.SuspendLayout();
            this.Tabs.SuspendLayout();
            this.TabServerControls.SuspendLayout();
            this.JoinOptionsGroupBox.SuspendLayout();
            this.WorldSelectGroupBox.SuspendLayout();
            this.TabAdvancedControls.SuspendLayout();
            this.SavingGroupBox.SuspendLayout();
            this.TabServerDetails.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.TabPlayers.SuspendLayout();
            this.TabLogs.SuspendLayout();
            this.TrayContextMenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // MenuStrip
            // 
            this.MenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItemFile,
            this.MenuItemHelp});
            this.MenuStrip.Location = new System.Drawing.Point(0, 0);
            this.MenuStrip.Name = "MenuStrip";
            this.MenuStrip.Size = new System.Drawing.Size(484, 24);
            this.MenuStrip.TabIndex = 0;
            // 
            // MenuItemFile
            // 
            this.MenuItemFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItemFileNewWindow,
            this.MenuItemFileSeparator3,
            this.MenuItemFileNewProfile,
            this.MenuItemFileSaveProfile,
            this.MenuItemFileSaveProfileAs,
            this.MenuItemFileLoadProfile,
            this.MenuItemFileRemoveProfile,
            this.MenuItemFileSeparator2,
            this.MenuItemFilePreferences,
            this.MenuItemFileDirectories,
            this.MenuItemFileOpenSettings,
            this.MenuItemFileSeparator1,
            this.MenuItemFileClose});
            this.MenuItemFile.Name = "MenuItemFile";
            this.MenuItemFile.Size = new System.Drawing.Size(37, 20);
            this.MenuItemFile.Text = "&File";
            // 
            // MenuItemFileNewWindow
            // 
            this.MenuItemFileNewWindow.Image = global::ValheimServerGUI.Properties.Resources.AddImmediateWindow_16x;
            this.MenuItemFileNewWindow.Name = "MenuItemFileNewWindow";
            this.MenuItemFileNewWindow.Size = new System.Drawing.Size(208, 22);
            this.MenuItemFileNewWindow.Text = "New &Window";
            // 
            // MenuItemFileSeparator3
            // 
            this.MenuItemFileSeparator3.Name = "MenuItemFileSeparator3";
            this.MenuItemFileSeparator3.Size = new System.Drawing.Size(205, 6);
            // 
            // MenuItemFileNewProfile
            // 
            this.MenuItemFileNewProfile.Image = global::ValheimServerGUI.Properties.Resources.NewFile_16x;
            this.MenuItemFileNewProfile.Name = "MenuItemFileNewProfile";
            this.MenuItemFileNewProfile.Size = new System.Drawing.Size(208, 22);
            this.MenuItemFileNewProfile.Text = "&New Profile";
            // 
            // MenuItemFileSaveProfile
            // 
            this.MenuItemFileSaveProfile.Image = global::ValheimServerGUI.Properties.Resources.Save_16x;
            this.MenuItemFileSaveProfile.Name = "MenuItemFileSaveProfile";
            this.MenuItemFileSaveProfile.Size = new System.Drawing.Size(208, 22);
            this.MenuItemFileSaveProfile.Text = "&Save Profile";
            // 
            // MenuItemFileSaveProfileAs
            // 
            this.MenuItemFileSaveProfileAs.Image = global::ValheimServerGUI.Properties.Resources.Save_16x;
            this.MenuItemFileSaveProfileAs.Name = "MenuItemFileSaveProfileAs";
            this.MenuItemFileSaveProfileAs.Size = new System.Drawing.Size(208, 22);
            this.MenuItemFileSaveProfileAs.Text = "Save Profile &As...";
            // 
            // MenuItemFileLoadProfile
            // 
            this.MenuItemFileLoadProfile.Image = global::ValheimServerGUI.Properties.Resources.OpenFile_16x;
            this.MenuItemFileLoadProfile.Name = "MenuItemFileLoadProfile";
            this.MenuItemFileLoadProfile.Size = new System.Drawing.Size(208, 22);
            this.MenuItemFileLoadProfile.Text = "&Load Profile";
            // 
            // MenuItemFileRemoveProfile
            // 
            this.MenuItemFileRemoveProfile.Image = global::ValheimServerGUI.Properties.Resources.Cancel_16x;
            this.MenuItemFileRemoveProfile.Name = "MenuItemFileRemoveProfile";
            this.MenuItemFileRemoveProfile.Size = new System.Drawing.Size(208, 22);
            this.MenuItemFileRemoveProfile.Text = "&Remove Profile";
            // 
            // MenuItemFileSeparator2
            // 
            this.MenuItemFileSeparator2.Name = "MenuItemFileSeparator2";
            this.MenuItemFileSeparator2.Size = new System.Drawing.Size(205, 6);
            // 
            // MenuItemFilePreferences
            // 
            this.MenuItemFilePreferences.Image = global::ValheimServerGUI.Properties.Resources.Settings_16x;
            this.MenuItemFilePreferences.Name = "MenuItemFilePreferences";
            this.MenuItemFilePreferences.Size = new System.Drawing.Size(208, 22);
            this.MenuItemFilePreferences.Text = "&Preferences...";
            // 
            // MenuItemFileDirectories
            // 
            this.MenuItemFileDirectories.Image = ((System.Drawing.Image)(resources.GetObject("MenuItemFileDirectories.Image")));
            this.MenuItemFileDirectories.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.MenuItemFileDirectories.Name = "MenuItemFileDirectories";
            this.MenuItemFileDirectories.Size = new System.Drawing.Size(208, 22);
            this.MenuItemFileDirectories.Text = "Set &Directories...";
            // 
            // MenuItemFileOpenSettings
            // 
            this.MenuItemFileOpenSettings.Image = global::ValheimServerGUI.Properties.Resources.OpenFolder_16x;
            this.MenuItemFileOpenSettings.Name = "MenuItemFileOpenSettings";
            this.MenuItemFileOpenSettings.Size = new System.Drawing.Size(208, 22);
            this.MenuItemFileOpenSettings.Text = "&Open Settings Directory...";
            // 
            // MenuItemFileSeparator1
            // 
            this.MenuItemFileSeparator1.Name = "MenuItemFileSeparator1";
            this.MenuItemFileSeparator1.Size = new System.Drawing.Size(205, 6);
            // 
            // MenuItemFileClose
            // 
            this.MenuItemFileClose.Name = "MenuItemFileClose";
            this.MenuItemFileClose.Size = new System.Drawing.Size(208, 22);
            this.MenuItemFileClose.Text = "&Close";
            // 
            // MenuItemHelp
            // 
            this.MenuItemHelp.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItemHelpManual,
            this.MenuItemHelpPortForwarding,
            this.MenuItemHelpBugReport,
            this.MenuItemHelpSeparator1,
            this.MenuItemHelpUpdates,
            this.MenuItemHelpAbout});
            this.MenuItemHelp.Name = "MenuItemHelp";
            this.MenuItemHelp.Size = new System.Drawing.Size(44, 20);
            this.MenuItemHelp.Text = "&Help";
            // 
            // MenuItemHelpManual
            // 
            this.MenuItemHelpManual.Image = ((System.Drawing.Image)(resources.GetObject("MenuItemHelpManual.Image")));
            this.MenuItemHelpManual.Name = "MenuItemHelpManual";
            this.MenuItemHelpManual.Size = new System.Drawing.Size(192, 22);
            this.MenuItemHelpManual.Text = "Online &Manual";
            // 
            // MenuItemHelpPortForwarding
            // 
            this.MenuItemHelpPortForwarding.Image = global::ValheimServerGUI.Properties.Resources.OpenWeb_16x;
            this.MenuItemHelpPortForwarding.Name = "MenuItemHelpPortForwarding";
            this.MenuItemHelpPortForwarding.Size = new System.Drawing.Size(192, 22);
            this.MenuItemHelpPortForwarding.Text = "&Port Forwarding";
            // 
            // MenuItemHelpBugReport
            // 
            this.MenuItemHelpBugReport.Image = global::ValheimServerGUI.Properties.Resources.NewBug_16x;
            this.MenuItemHelpBugReport.Name = "MenuItemHelpBugReport";
            this.MenuItemHelpBugReport.Size = new System.Drawing.Size(192, 22);
            this.MenuItemHelpBugReport.Text = "Submit a &Bug Report...";
            // 
            // MenuItemHelpSeparator1
            // 
            this.MenuItemHelpSeparator1.Name = "MenuItemHelpSeparator1";
            this.MenuItemHelpSeparator1.Size = new System.Drawing.Size(189, 6);
            // 
            // MenuItemHelpUpdates
            // 
            this.MenuItemHelpUpdates.Image = global::ValheimServerGUI.Properties.Resources.UnsyncedCommits_16x_Horiz;
            this.MenuItemHelpUpdates.Name = "MenuItemHelpUpdates";
            this.MenuItemHelpUpdates.Size = new System.Drawing.Size(192, 22);
            this.MenuItemHelpUpdates.Text = "Check for &Updates";
            // 
            // MenuItemHelpAbout
            // 
            this.MenuItemHelpAbout.Name = "MenuItemHelpAbout";
            this.MenuItemHelpAbout.Size = new System.Drawing.Size(192, 22);
            this.MenuItemHelpAbout.Text = "&About...";
            // 
            // StatusStrip
            // 
            this.StatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.StatusStripLabelLeft,
            this.StatusStripLabelSpacer,
            this.StatusStripLabelRight});
            this.StatusStrip.Location = new System.Drawing.Point(0, 310);
            this.StatusStrip.Name = "StatusStrip";
            this.StatusStrip.Size = new System.Drawing.Size(484, 22);
            this.StatusStrip.TabIndex = 1;
            // 
            // StatusStripLabelLeft
            // 
            this.StatusStripLabelLeft.Name = "StatusStripLabelLeft";
            this.StatusStripLabelLeft.Size = new System.Drawing.Size(0, 17);
            // 
            // StatusStripLabelSpacer
            // 
            this.StatusStripLabelSpacer.Name = "StatusStripLabelSpacer";
            this.StatusStripLabelSpacer.Size = new System.Drawing.Size(469, 17);
            this.StatusStripLabelSpacer.Spring = true;
            // 
            // StatusStripLabelRight
            // 
            this.StatusStripLabelRight.Name = "StatusStripLabelRight";
            this.StatusStripLabelRight.Size = new System.Drawing.Size(0, 17);
            // 
            // Tabs
            // 
            this.Tabs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Tabs.Controls.Add(this.TabServerControls);
            this.Tabs.Controls.Add(this.TabAdvancedControls);
            this.Tabs.Controls.Add(this.TabServerDetails);
            this.Tabs.Controls.Add(this.TabPlayers);
            this.Tabs.Controls.Add(this.TabLogs);
            this.Tabs.Location = new System.Drawing.Point(12, 27);
            this.Tabs.Name = "Tabs";
            this.Tabs.SelectedIndex = 0;
            this.Tabs.Size = new System.Drawing.Size(460, 280);
            this.Tabs.TabIndex = 2;
            // 
            // TabServerControls
            // 
            this.TabServerControls.Controls.Add(this.CopyButtonServerPassword);
            this.TabServerControls.Controls.Add(this.JoinOptionsGroupBox);
            this.TabServerControls.Controls.Add(this.WorldSelectGroupBox);
            this.TabServerControls.Controls.Add(this.ServerPortField);
            this.TabServerControls.Controls.Add(this.ButtonRestartServer);
            this.TabServerControls.Controls.Add(this.ShowPasswordField);
            this.TabServerControls.Controls.Add(this.ServerPasswordField);
            this.TabServerControls.Controls.Add(this.ServerNameField);
            this.TabServerControls.Controls.Add(this.ButtonStopServer);
            this.TabServerControls.Controls.Add(this.ButtonStartServer);
            this.TabServerControls.Location = new System.Drawing.Point(4, 24);
            this.TabServerControls.Name = "TabServerControls";
            this.TabServerControls.Padding = new System.Windows.Forms.Padding(3);
            this.TabServerControls.Size = new System.Drawing.Size(452, 252);
            this.TabServerControls.TabIndex = 0;
            this.TabServerControls.Text = "Server Controls";
            this.TabServerControls.UseVisualStyleBackColor = true;
            // 
            // CopyButtonServerPassword
            // 
            this.CopyButtonServerPassword.CopyFunction = null;
            this.CopyButtonServerPassword.Location = new System.Drawing.Point(236, 69);
            this.CopyButtonServerPassword.Name = "CopyButtonServerPassword";
            this.CopyButtonServerPassword.Size = new System.Drawing.Size(16, 16);
            this.CopyButtonServerPassword.TabIndex = 21;
            // 
            // JoinOptionsGroupBox
            // 
            this.JoinOptionsGroupBox.Controls.Add(this.CommunityServerField);
            this.JoinOptionsGroupBox.Controls.Add(this.ServerCrossplayField);
            this.JoinOptionsGroupBox.Location = new System.Drawing.Point(249, 94);
            this.JoinOptionsGroupBox.Name = "JoinOptionsGroupBox";
            this.JoinOptionsGroupBox.Size = new System.Drawing.Size(197, 96);
            this.JoinOptionsGroupBox.TabIndex = 19;
            this.JoinOptionsGroupBox.TabStop = false;
            this.JoinOptionsGroupBox.Text = "Join Options";
            // 
            // CommunityServerField
            // 
            this.CommunityServerField.HelpText = resources.GetString("CommunityServerField.HelpText");
            this.CommunityServerField.LabelText = "Community Server";
            this.CommunityServerField.Location = new System.Drawing.Point(6, 22);
            this.CommunityServerField.Name = "CommunityServerField";
            this.CommunityServerField.Size = new System.Drawing.Size(142, 17);
            this.CommunityServerField.TabIndex = 13;
            this.CommunityServerField.Value = false;
            // 
            // ServerCrossplayField
            // 
            this.ServerCrossplayField.HelpText = "Allow players on other platforms to join your game\r\n(Microsoft Store, Xbox, etc.)" +
    "\r\n\r\nYou may need to provide other players an Invite Code,\r\nwhich appears in game" +
    " and in the server logs.";
            this.ServerCrossplayField.LabelText = "Enable Crossplay";
            this.ServerCrossplayField.Location = new System.Drawing.Point(6, 45);
            this.ServerCrossplayField.Name = "ServerCrossplayField";
            this.ServerCrossplayField.Size = new System.Drawing.Size(133, 17);
            this.ServerCrossplayField.TabIndex = 18;
            this.ServerCrossplayField.Value = false;
            // 
            // WorldSelectGroupBox
            // 
            this.WorldSelectGroupBox.Controls.Add(this.WorldSelectNewNameField);
            this.WorldSelectGroupBox.Controls.Add(this.WorldSelectRadioNew);
            this.WorldSelectGroupBox.Controls.Add(this.WorldSelectRadioExisting);
            this.WorldSelectGroupBox.Controls.Add(this.WorldSelectExistingNameField);
            this.WorldSelectGroupBox.Location = new System.Drawing.Point(3, 94);
            this.WorldSelectGroupBox.Name = "WorldSelectGroupBox";
            this.WorldSelectGroupBox.Size = new System.Drawing.Size(240, 96);
            this.WorldSelectGroupBox.TabIndex = 17;
            this.WorldSelectGroupBox.TabStop = false;
            this.WorldSelectGroupBox.Text = "World";
            // 
            // WorldSelectNewNameField
            // 
            this.WorldSelectNewNameField.HelpText = "";
            this.WorldSelectNewNameField.HideValue = false;
            this.WorldSelectNewNameField.LabelText = "New World Name";
            this.WorldSelectNewNameField.Location = new System.Drawing.Point(6, 45);
            this.WorldSelectNewNameField.MaxLength = 20;
            this.WorldSelectNewNameField.Multiline = false;
            this.WorldSelectNewNameField.Name = "WorldSelectNewNameField";
            this.WorldSelectNewNameField.Size = new System.Drawing.Size(234, 41);
            this.WorldSelectNewNameField.TabIndex = 18;
            this.WorldSelectNewNameField.Value = "";
            this.WorldSelectNewNameField.Visible = false;
            // 
            // WorldSelectRadioNew
            // 
            this.WorldSelectRadioNew.GroupName = "WorldSelect";
            this.WorldSelectRadioNew.HelpText = "";
            this.WorldSelectRadioNew.LabelText = "Create New";
            this.WorldSelectRadioNew.Location = new System.Drawing.Point(117, 22);
            this.WorldSelectRadioNew.Name = "WorldSelectRadioNew";
            this.WorldSelectRadioNew.Size = new System.Drawing.Size(111, 17);
            this.WorldSelectRadioNew.TabIndex = 14;
            this.WorldSelectRadioNew.Value = false;
            // 
            // WorldSelectRadioExisting
            // 
            this.WorldSelectRadioExisting.GroupName = "WorldSelect";
            this.WorldSelectRadioExisting.HelpText = "";
            this.WorldSelectRadioExisting.LabelText = "Use Existing";
            this.WorldSelectRadioExisting.Location = new System.Drawing.Point(6, 22);
            this.WorldSelectRadioExisting.Name = "WorldSelectRadioExisting";
            this.WorldSelectRadioExisting.Size = new System.Drawing.Size(105, 17);
            this.WorldSelectRadioExisting.TabIndex = 13;
            this.WorldSelectRadioExisting.Value = false;
            // 
            // WorldSelectExistingNameField
            // 
            this.WorldSelectExistingNameField.DataSource = ((System.Collections.Generic.IEnumerable<string>)(resources.GetObject("WorldSelectExistingNameField.DataSource")));
            this.WorldSelectExistingNameField.DropdownEnabled = true;
            this.WorldSelectExistingNameField.EmptyText = "(no worlds)";
            this.WorldSelectExistingNameField.HelpText = "";
            this.WorldSelectExistingNameField.LabelText = "Select World";
            this.WorldSelectExistingNameField.Location = new System.Drawing.Point(6, 45);
            this.WorldSelectExistingNameField.Name = "WorldSelectExistingNameField";
            this.WorldSelectExistingNameField.Size = new System.Drawing.Size(234, 41);
            this.WorldSelectExistingNameField.TabIndex = 12;
            this.WorldSelectExistingNameField.Value = null;
            this.WorldSelectExistingNameField.Visible = false;
            // 
            // ServerPortField
            // 
            this.ServerPortField.HelpText = "";
            this.ServerPortField.LabelText = "Port";
            this.ServerPortField.Location = new System.Drawing.Point(234, 0);
            this.ServerPortField.Maximum = 65535;
            this.ServerPortField.Minimum = 1;
            this.ServerPortField.Name = "ServerPortField";
            this.ServerPortField.Size = new System.Drawing.Size(75, 41);
            this.ServerPortField.TabIndex = 16;
            this.ServerPortField.TabStop = false;
            this.ServerPortField.Value = 1;
            // 
            // ButtonRestartServer
            // 
            this.ButtonRestartServer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ButtonRestartServer.Image = ((System.Drawing.Image)(resources.GetObject("ButtonRestartServer.Image")));
            this.ButtonRestartServer.Location = new System.Drawing.Point(115, 226);
            this.ButtonRestartServer.Name = "ButtonRestartServer";
            this.ButtonRestartServer.Size = new System.Drawing.Size(106, 23);
            this.ButtonRestartServer.TabIndex = 15;
            this.ButtonRestartServer.Text = "Restart Server";
            this.ButtonRestartServer.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.ButtonRestartServer.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.ButtonRestartServer.UseVisualStyleBackColor = true;
            // 
            // ShowPasswordField
            // 
            this.ShowPasswordField.HelpText = "";
            this.ShowPasswordField.LabelText = "Show Password";
            this.ShowPasswordField.Location = new System.Drawing.Point(255, 68);
            this.ShowPasswordField.Name = "ShowPasswordField";
            this.ShowPasswordField.Size = new System.Drawing.Size(150, 17);
            this.ShowPasswordField.TabIndex = 14;
            this.ShowPasswordField.Value = false;
            // 
            // ServerPasswordField
            // 
            this.ServerPasswordField.HelpText = "Your server must be protected with a password. The password must be at least 5 ch" +
    "aracters \r\nand must not contain the name of the server or the world that you\'re " +
    "hosting.";
            this.ServerPasswordField.HideValue = true;
            this.ServerPasswordField.LabelText = "Server Password";
            this.ServerPasswordField.Location = new System.Drawing.Point(0, 47);
            this.ServerPasswordField.MaxLength = 64;
            this.ServerPasswordField.Multiline = false;
            this.ServerPasswordField.Name = "ServerPasswordField";
            this.ServerPasswordField.Size = new System.Drawing.Size(243, 41);
            this.ServerPasswordField.TabIndex = 11;
            this.ServerPasswordField.Value = "";
            // 
            // ServerNameField
            // 
            this.ServerNameField.HelpText = "This is the name that will appear in the Community Servers list within Valheim.";
            this.ServerNameField.HideValue = false;
            this.ServerNameField.LabelText = "Server Name";
            this.ServerNameField.Location = new System.Drawing.Point(0, 0);
            this.ServerNameField.MaxLength = 64;
            this.ServerNameField.Multiline = false;
            this.ServerNameField.Name = "ServerNameField";
            this.ServerNameField.Size = new System.Drawing.Size(243, 41);
            this.ServerNameField.TabIndex = 10;
            this.ServerNameField.Value = "";
            // 
            // ButtonStopServer
            // 
            this.ButtonStopServer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ButtonStopServer.Image = ((System.Drawing.Image)(resources.GetObject("ButtonStopServer.Image")));
            this.ButtonStopServer.Location = new System.Drawing.Point(227, 226);
            this.ButtonStopServer.Name = "ButtonStopServer";
            this.ButtonStopServer.Size = new System.Drawing.Size(106, 23);
            this.ButtonStopServer.TabIndex = 1;
            this.ButtonStopServer.Text = "Stop Server";
            this.ButtonStopServer.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.ButtonStopServer.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.ButtonStopServer.UseVisualStyleBackColor = true;
            // 
            // ButtonStartServer
            // 
            this.ButtonStartServer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ButtonStartServer.Image = ((System.Drawing.Image)(resources.GetObject("ButtonStartServer.Image")));
            this.ButtonStartServer.Location = new System.Drawing.Point(3, 226);
            this.ButtonStartServer.Name = "ButtonStartServer";
            this.ButtonStartServer.Size = new System.Drawing.Size(106, 23);
            this.ButtonStartServer.TabIndex = 0;
            this.ButtonStartServer.Text = "Start Server";
            this.ButtonStartServer.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.ButtonStartServer.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.ButtonStartServer.UseVisualStyleBackColor = true;
            // 
            // TabAdvancedControls
            // 
            this.TabAdvancedControls.Controls.Add(this.ServerAdditionalArgsField);
            this.TabAdvancedControls.Controls.Add(this.SavingGroupBox);
            this.TabAdvancedControls.Location = new System.Drawing.Point(4, 24);
            this.TabAdvancedControls.Name = "TabAdvancedControls";
            this.TabAdvancedControls.Padding = new System.Windows.Forms.Padding(3);
            this.TabAdvancedControls.Size = new System.Drawing.Size(452, 252);
            this.TabAdvancedControls.TabIndex = 5;
            this.TabAdvancedControls.Text = "Advanced Controls";
            this.TabAdvancedControls.UseVisualStyleBackColor = true;
            // 
            // ServerAdditionalArgsField
            // 
            this.ServerAdditionalArgsField.HelpText = "Add any additional args you want to pass to the server run\r\ncommand here. These w" +
    "ill be appended to the end of the\r\ncommand generated by ValheimServerGUI.";
            this.ServerAdditionalArgsField.HideValue = false;
            this.ServerAdditionalArgsField.LabelText = "Additional Command Line Args";
            this.ServerAdditionalArgsField.Location = new System.Drawing.Point(12, 141);
            this.ServerAdditionalArgsField.MaxLength = 32767;
            this.ServerAdditionalArgsField.Multiline = false;
            this.ServerAdditionalArgsField.Name = "ServerAdditionalArgsField";
            this.ServerAdditionalArgsField.Size = new System.Drawing.Size(295, 41);
            this.ServerAdditionalArgsField.TabIndex = 1;
            this.ServerAdditionalArgsField.Value = "";
            // 
            // SavingGroupBox
            // 
            this.SavingGroupBox.Controls.Add(this.ServerLongBackupIntervalField);
            this.SavingGroupBox.Controls.Add(this.ServerShortBackupIntervalField);
            this.SavingGroupBox.Controls.Add(this.ServerBackupsField);
            this.SavingGroupBox.Controls.Add(this.ServerSaveIntervalField);
            this.SavingGroupBox.Location = new System.Drawing.Point(6, 6);
            this.SavingGroupBox.Name = "SavingGroupBox";
            this.SavingGroupBox.Size = new System.Drawing.Size(307, 129);
            this.SavingGroupBox.TabIndex = 0;
            this.SavingGroupBox.TabStop = false;
            this.SavingGroupBox.Text = "Saving and Backups";
            // 
            // ServerLongBackupIntervalField
            // 
            this.ServerLongBackupIntervalField.HelpText = "How often to create additional backups of the world\r\nsave data, in seconds. This " +
    "interval must be longer than\r\nthe short backup interval.";
            this.ServerLongBackupIntervalField.LabelText = "Long Backup Interval";
            this.ServerLongBackupIntervalField.Location = new System.Drawing.Point(128, 69);
            this.ServerLongBackupIntervalField.Maximum = 2592000;
            this.ServerLongBackupIntervalField.Minimum = 300;
            this.ServerLongBackupIntervalField.Name = "ServerLongBackupIntervalField";
            this.ServerLongBackupIntervalField.Size = new System.Drawing.Size(173, 41);
            this.ServerLongBackupIntervalField.TabIndex = 1;
            this.ServerLongBackupIntervalField.Value = 300;
            // 
            // ServerShortBackupIntervalField
            // 
            this.ServerShortBackupIntervalField.HelpText = "How often to create a rolling backup of the world\r\nsave data, in seconds.";
            this.ServerShortBackupIntervalField.LabelText = "Short Backup Interval";
            this.ServerShortBackupIntervalField.Location = new System.Drawing.Point(128, 22);
            this.ServerShortBackupIntervalField.Maximum = 2592000;
            this.ServerShortBackupIntervalField.Minimum = 300;
            this.ServerShortBackupIntervalField.Name = "ServerShortBackupIntervalField";
            this.ServerShortBackupIntervalField.Size = new System.Drawing.Size(173, 41);
            this.ServerShortBackupIntervalField.TabIndex = 2;
            this.ServerShortBackupIntervalField.Value = 300;
            // 
            // ServerBackupsField
            // 
            this.ServerBackupsField.HelpText = "Number of world data backups to maintain. One rolling backup\r\nis created on the s" +
    "hort backup interval, and subsequent backups are\r\ncreated on the long backup int" +
    "erval.";
            this.ServerBackupsField.LabelText = "Backups";
            this.ServerBackupsField.Location = new System.Drawing.Point(6, 69);
            this.ServerBackupsField.Maximum = 1000;
            this.ServerBackupsField.Minimum = 1;
            this.ServerBackupsField.Name = "ServerBackupsField";
            this.ServerBackupsField.Size = new System.Drawing.Size(116, 41);
            this.ServerBackupsField.TabIndex = 1;
            this.ServerBackupsField.Value = 1;
            // 
            // ServerSaveIntervalField
            // 
            this.ServerSaveIntervalField.HelpText = "How often the world is saved, in seconds.";
            this.ServerSaveIntervalField.LabelText = "Save Interval";
            this.ServerSaveIntervalField.Location = new System.Drawing.Point(6, 22);
            this.ServerSaveIntervalField.Maximum = 86400;
            this.ServerSaveIntervalField.Minimum = 60;
            this.ServerSaveIntervalField.Name = "ServerSaveIntervalField";
            this.ServerSaveIntervalField.Size = new System.Drawing.Size(116, 41);
            this.ServerSaveIntervalField.TabIndex = 0;
            this.ServerSaveIntervalField.Value = 60;
            // 
            // TabServerDetails
            // 
            this.TabServerDetails.Controls.Add(this.groupBox2);
            this.TabServerDetails.Controls.Add(this.groupBox1);
            this.TabServerDetails.Location = new System.Drawing.Point(4, 24);
            this.TabServerDetails.Name = "TabServerDetails";
            this.TabServerDetails.Size = new System.Drawing.Size(452, 252);
            this.TabServerDetails.TabIndex = 4;
            this.TabServerDetails.Text = "Server Details";
            this.TabServerDetails.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.LabelSessionDuration);
            this.groupBox2.Controls.Add(this.LabelAverageWorldSave);
            this.groupBox2.Controls.Add(this.LabelLastWorldSave);
            this.groupBox2.Location = new System.Drawing.Point(3, 143);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(307, 100);
            this.groupBox2.TabIndex = 4;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Statistics";
            // 
            // LabelSessionDuration
            // 
            this.LabelSessionDuration.HelpText = "";
            this.LabelSessionDuration.LabelSplitRatio = 0.5D;
            this.LabelSessionDuration.LabelText = "Server Uptime:";
            this.LabelSessionDuration.LabelTextAlign = System.Drawing.ContentAlignment.TopRight;
            this.LabelSessionDuration.Location = new System.Drawing.Point(7, 23);
            this.LabelSessionDuration.Name = "LabelSessionDuration";
            this.LabelSessionDuration.Size = new System.Drawing.Size(241, 15);
            this.LabelSessionDuration.TabIndex = 2;
            this.LabelSessionDuration.Value = "";
            this.LabelSessionDuration.ValueTextAlign = System.Drawing.ContentAlignment.TopLeft;
            // 
            // LabelAverageWorldSave
            // 
            this.LabelAverageWorldSave.HelpText = "The average amount of time it has taken to save\r\nyour game world, over the past 1" +
    "0 saves.";
            this.LabelAverageWorldSave.LabelSplitRatio = 0.455D;
            this.LabelAverageWorldSave.LabelText = "Avg. World Save:";
            this.LabelAverageWorldSave.LabelTextAlign = System.Drawing.ContentAlignment.TopRight;
            this.LabelAverageWorldSave.Location = new System.Drawing.Point(6, 65);
            this.LabelAverageWorldSave.Name = "LabelAverageWorldSave";
            this.LabelAverageWorldSave.Size = new System.Drawing.Size(264, 15);
            this.LabelAverageWorldSave.TabIndex = 1;
            this.LabelAverageWorldSave.Value = "";
            this.LabelAverageWorldSave.ValueTextAlign = System.Drawing.ContentAlignment.TopLeft;
            // 
            // LabelLastWorldSave
            // 
            this.LabelLastWorldSave.HelpText = "";
            this.LabelLastWorldSave.LabelSplitRatio = 0.405D;
            this.LabelLastWorldSave.LabelText = "Last World Save:";
            this.LabelLastWorldSave.LabelTextAlign = System.Drawing.ContentAlignment.TopRight;
            this.LabelLastWorldSave.Location = new System.Drawing.Point(6, 44);
            this.LabelLastWorldSave.Name = "LabelLastWorldSave";
            this.LabelLastWorldSave.Size = new System.Drawing.Size(295, 15);
            this.LabelLastWorldSave.TabIndex = 0;
            this.LabelLastWorldSave.Value = "";
            this.LabelLastWorldSave.ValueTextAlign = System.Drawing.ContentAlignment.TopLeft;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.CopyButtonInviteCode);
            this.groupBox1.Controls.Add(this.LabelInviteCode);
            this.groupBox1.Controls.Add(this.CopyButtonLocalIpAddress);
            this.groupBox1.Controls.Add(this.CopyButtonExternalIpAddress);
            this.groupBox1.Controls.Add(this.CopyButtonInternalIpAddress);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.LabelExternalIpAddress);
            this.groupBox1.Controls.Add(this.LabelLocalIpAddress);
            this.groupBox1.Controls.Add(this.LabelInternalIpAddress);
            this.groupBox1.Location = new System.Drawing.Point(3, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(307, 134);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Connection Details";
            // 
            // CopyButtonInviteCode
            // 
            this.CopyButtonInviteCode.CopyFunction = null;
            this.CopyButtonInviteCode.Location = new System.Drawing.Point(276, 85);
            this.CopyButtonInviteCode.Name = "CopyButtonInviteCode";
            this.CopyButtonInviteCode.Size = new System.Drawing.Size(16, 16);
            this.CopyButtonInviteCode.TabIndex = 8;
            // 
            // LabelInviteCode
            // 
            this.LabelInviteCode.HelpText = resources.GetString("LabelInviteCode.HelpText");
            this.LabelInviteCode.LabelSplitRatio = 0.455D;
            this.LabelInviteCode.LabelText = "Invite Code:";
            this.LabelInviteCode.LabelTextAlign = System.Drawing.ContentAlignment.TopRight;
            this.LabelInviteCode.Location = new System.Drawing.Point(6, 85);
            this.LabelInviteCode.Name = "LabelInviteCode";
            this.LabelInviteCode.Size = new System.Drawing.Size(264, 15);
            this.LabelInviteCode.TabIndex = 7;
            this.LabelInviteCode.Value = "N/A";
            this.LabelInviteCode.ValueTextAlign = System.Drawing.ContentAlignment.TopLeft;
            // 
            // CopyButtonLocalIpAddress
            // 
            this.CopyButtonLocalIpAddress.CopyFunction = null;
            this.CopyButtonLocalIpAddress.Location = new System.Drawing.Point(276, 63);
            this.CopyButtonLocalIpAddress.Name = "CopyButtonLocalIpAddress";
            this.CopyButtonLocalIpAddress.Size = new System.Drawing.Size(16, 16);
            this.CopyButtonLocalIpAddress.TabIndex = 5;
            // 
            // CopyButtonExternalIpAddress
            // 
            this.CopyButtonExternalIpAddress.CopyFunction = null;
            this.CopyButtonExternalIpAddress.Location = new System.Drawing.Point(276, 22);
            this.CopyButtonExternalIpAddress.Name = "CopyButtonExternalIpAddress";
            this.CopyButtonExternalIpAddress.Size = new System.Drawing.Size(16, 16);
            this.CopyButtonExternalIpAddress.TabIndex = 4;
            // 
            // CopyButtonInternalIpAddress
            // 
            this.CopyButtonInternalIpAddress.CopyFunction = null;
            this.CopyButtonInternalIpAddress.Location = new System.Drawing.Point(276, 43);
            this.CopyButtonInternalIpAddress.Name = "CopyButtonInternalIpAddress";
            this.CopyButtonInternalIpAddress.Size = new System.Drawing.Size(16, 16);
            this.CopyButtonInternalIpAddress.TabIndex = 6;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point);
            this.label1.Location = new System.Drawing.Point(6, 110);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(261, 15);
            this.label1.TabIndex = 3;
            this.label1.Text = "Trouble connecting? See Help > Port Forwarding.\r\n";
            // 
            // LabelExternalIpAddress
            // 
            this.LabelExternalIpAddress.HelpText = "This is the address that players from outside your home network will use to\r\nconn" +
    "ect to your server. Give this address to your friends for standard online play.\r" +
    "\n";
            this.LabelExternalIpAddress.LabelSplitRatio = 0.455D;
            this.LabelExternalIpAddress.LabelText = "External IP Address:";
            this.LabelExternalIpAddress.LabelTextAlign = System.Drawing.ContentAlignment.TopRight;
            this.LabelExternalIpAddress.Location = new System.Drawing.Point(6, 22);
            this.LabelExternalIpAddress.Name = "LabelExternalIpAddress";
            this.LabelExternalIpAddress.Size = new System.Drawing.Size(264, 15);
            this.LabelExternalIpAddress.TabIndex = 0;
            this.LabelExternalIpAddress.Value = "Loading...";
            this.LabelExternalIpAddress.ValueTextAlign = System.Drawing.ContentAlignment.TopLeft;
            // 
            // LabelLocalIpAddress
            // 
            this.LabelLocalIpAddress.HelpText = resources.GetString("LabelLocalIpAddress.HelpText");
            this.LabelLocalIpAddress.LabelSplitRatio = 0.455D;
            this.LabelLocalIpAddress.LabelText = "Local IP Address:";
            this.LabelLocalIpAddress.LabelTextAlign = System.Drawing.ContentAlignment.TopRight;
            this.LabelLocalIpAddress.Location = new System.Drawing.Point(6, 64);
            this.LabelLocalIpAddress.Name = "LabelLocalIpAddress";
            this.LabelLocalIpAddress.Size = new System.Drawing.Size(264, 15);
            this.LabelLocalIpAddress.TabIndex = 2;
            this.LabelLocalIpAddress.Value = "127.0.0.1";
            this.LabelLocalIpAddress.ValueTextAlign = System.Drawing.ContentAlignment.TopLeft;
            // 
            // LabelInternalIpAddress
            // 
            this.LabelInternalIpAddress.HelpText = resources.GetString("LabelInternalIpAddress.HelpText");
            this.LabelInternalIpAddress.LabelSplitRatio = 0.455D;
            this.LabelInternalIpAddress.LabelText = "Internal IP Address:";
            this.LabelInternalIpAddress.LabelTextAlign = System.Drawing.ContentAlignment.TopRight;
            this.LabelInternalIpAddress.Location = new System.Drawing.Point(6, 43);
            this.LabelInternalIpAddress.Name = "LabelInternalIpAddress";
            this.LabelInternalIpAddress.Size = new System.Drawing.Size(264, 15);
            this.LabelInternalIpAddress.TabIndex = 1;
            this.LabelInternalIpAddress.Value = "Loading...";
            this.LabelInternalIpAddress.ValueTextAlign = System.Drawing.ContentAlignment.TopLeft;
            // 
            // TabPlayers
            // 
            this.TabPlayers.Controls.Add(this.ButtonRemovePlayer);
            this.TabPlayers.Controls.Add(this.ButtonPlayerDetails);
            this.TabPlayers.Controls.Add(this.PlayersTable);
            this.TabPlayers.Location = new System.Drawing.Point(4, 24);
            this.TabPlayers.Name = "TabPlayers";
            this.TabPlayers.Size = new System.Drawing.Size(452, 252);
            this.TabPlayers.TabIndex = 3;
            this.TabPlayers.Text = "Players";
            this.TabPlayers.UseVisualStyleBackColor = true;
            // 
            // ButtonRemovePlayer
            // 
            this.ButtonRemovePlayer.Enabled = false;
            this.ButtonRemovePlayer.Image = global::ValheimServerGUI.Properties.Resources.Cancel_16x;
            this.ButtonRemovePlayer.Location = new System.Drawing.Point(426, 3);
            this.ButtonRemovePlayer.Name = "ButtonRemovePlayer";
            this.ButtonRemovePlayer.Size = new System.Drawing.Size(23, 23);
            this.ButtonRemovePlayer.TabIndex = 2;
            this.ButtonRemovePlayer.UseVisualStyleBackColor = true;
            // 
            // ButtonPlayerDetails
            // 
            this.ButtonPlayerDetails.Enabled = false;
            this.ButtonPlayerDetails.Location = new System.Drawing.Point(3, 3);
            this.ButtonPlayerDetails.Name = "ButtonPlayerDetails";
            this.ButtonPlayerDetails.Size = new System.Drawing.Size(92, 23);
            this.ButtonPlayerDetails.TabIndex = 1;
            this.ButtonPlayerDetails.Text = "Player Info...";
            this.ButtonPlayerDetails.UseVisualStyleBackColor = true;
            // 
            // PlayersTable
            // 
            this.PlayersTable.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PlayersTable.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.ColumnPlayerStatus,
            this.ColumnPlayerName,
            this.ColumnPlayerUpdated});
            this.PlayersTable.Icons = this.ImageList;
            this.PlayersTable.Location = new System.Drawing.Point(3, 32);
            this.PlayersTable.Name = "PlayersTable";
            this.PlayersTable.Size = new System.Drawing.Size(446, 217);
            this.PlayersTable.TabIndex = 0;
            // 
            // ColumnPlayerStatus
            // 
            this.ColumnPlayerStatus.DisplayIndex = 1;
            this.ColumnPlayerStatus.Text = "Status";
            this.ColumnPlayerStatus.Width = 120;
            // 
            // ColumnPlayerName
            // 
            this.ColumnPlayerName.DisplayIndex = 0;
            this.ColumnPlayerName.Text = "Character Name";
            this.ColumnPlayerName.Width = 160;
            // 
            // ColumnPlayerUpdated
            // 
            this.ColumnPlayerUpdated.Text = "Since";
            this.ColumnPlayerUpdated.Width = 160;
            // 
            // ImageList
            // 
            this.ImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            this.ImageList.ImageSize = new System.Drawing.Size(16, 16);
            this.ImageList.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // TabLogs
            // 
            this.TabLogs.Controls.Add(this.ButtonSaveLogs);
            this.TabLogs.Controls.Add(this.LogViewSelectField);
            this.TabLogs.Controls.Add(this.LogViewer);
            this.TabLogs.Controls.Add(this.ButtonClearLogs);
            this.TabLogs.Location = new System.Drawing.Point(4, 24);
            this.TabLogs.Name = "TabLogs";
            this.TabLogs.Size = new System.Drawing.Size(452, 252);
            this.TabLogs.TabIndex = 2;
            this.TabLogs.Text = "Logs";
            this.TabLogs.UseVisualStyleBackColor = true;
            // 
            // ButtonSaveLogs
            // 
            this.ButtonSaveLogs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonSaveLogs.Location = new System.Drawing.Point(280, 22);
            this.ButtonSaveLogs.Name = "ButtonSaveLogs";
            this.ButtonSaveLogs.Size = new System.Drawing.Size(88, 23);
            this.ButtonSaveLogs.TabIndex = 4;
            this.ButtonSaveLogs.Text = "Save Logs...";
            this.ButtonSaveLogs.UseVisualStyleBackColor = true;
            // 
            // LogViewSelectField
            // 
            this.LogViewSelectField.DropdownEnabled = true;
            this.LogViewSelectField.EmptyText = "";
            this.LogViewSelectField.HelpText = "";
            this.LogViewSelectField.LabelText = "View logs for...";
            this.LogViewSelectField.Location = new System.Drawing.Point(-4, 4);
            this.LogViewSelectField.Name = "LogViewSelectField";
            this.LogViewSelectField.Size = new System.Drawing.Size(150, 41);
            this.LogViewSelectField.TabIndex = 3;
            this.LogViewSelectField.Value = null;
            // 
            // LogViewer
            // 
            this.LogViewer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LogViewer.Location = new System.Drawing.Point(3, 51);
            this.LogViewer.LogView = "DefaultLogView";
            this.LogViewer.Name = "LogViewer";
            this.LogViewer.Size = new System.Drawing.Size(446, 198);
            this.LogViewer.TabIndex = 2;
            this.LogViewer.TimestampFormat = "T";
            // 
            // ButtonClearLogs
            // 
            this.ButtonClearLogs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonClearLogs.Location = new System.Drawing.Point(374, 22);
            this.ButtonClearLogs.Name = "ButtonClearLogs";
            this.ButtonClearLogs.Size = new System.Drawing.Size(75, 23);
            this.ButtonClearLogs.TabIndex = 1;
            this.ButtonClearLogs.Text = "Clear Logs";
            this.ButtonClearLogs.UseVisualStyleBackColor = true;
            // 
            // NotifyIcon
            // 
            this.NotifyIcon.ContextMenuStrip = this.TrayContextMenuStrip;
            this.NotifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("NotifyIcon.Icon")));
            this.NotifyIcon.Text = "ValheimServerGUI";
            this.NotifyIcon.Visible = true;
            // 
            // TrayContextMenuStrip
            // 
            this.TrayContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.TrayContextMenuServerName,
            this.TrayContextMenuSeparator2,
            this.TrayContextMenuStart,
            this.TrayContextMenuRestart,
            this.TrayContextMenuStop,
            this.TrayContextMenuSeparator1,
            this.TrayContextMenuClose});
            this.TrayContextMenuStrip.Name = "TrayContextMenuStrip";
            this.TrayContextMenuStrip.Size = new System.Drawing.Size(146, 126);
            // 
            // TrayContextMenuServerName
            // 
            this.TrayContextMenuServerName.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.TrayContextMenuServerName.Name = "TrayContextMenuServerName";
            this.TrayContextMenuServerName.Size = new System.Drawing.Size(145, 22);
            this.TrayContextMenuServerName.Text = "ServerName";
            // 
            // TrayContextMenuSeparator2
            // 
            this.TrayContextMenuSeparator2.Name = "TrayContextMenuSeparator2";
            this.TrayContextMenuSeparator2.Size = new System.Drawing.Size(142, 6);
            // 
            // TrayContextMenuStart
            // 
            this.TrayContextMenuStart.Enabled = false;
            this.TrayContextMenuStart.Image = ((System.Drawing.Image)(resources.GetObject("TrayContextMenuStart.Image")));
            this.TrayContextMenuStart.Name = "TrayContextMenuStart";
            this.TrayContextMenuStart.Size = new System.Drawing.Size(145, 22);
            this.TrayContextMenuStart.Text = "Start Server";
            // 
            // TrayContextMenuRestart
            // 
            this.TrayContextMenuRestart.Enabled = false;
            this.TrayContextMenuRestart.Image = ((System.Drawing.Image)(resources.GetObject("TrayContextMenuRestart.Image")));
            this.TrayContextMenuRestart.Name = "TrayContextMenuRestart";
            this.TrayContextMenuRestart.Size = new System.Drawing.Size(145, 22);
            this.TrayContextMenuRestart.Text = "Restart Server";
            // 
            // TrayContextMenuStop
            // 
            this.TrayContextMenuStop.Enabled = false;
            this.TrayContextMenuStop.Image = ((System.Drawing.Image)(resources.GetObject("TrayContextMenuStop.Image")));
            this.TrayContextMenuStop.Name = "TrayContextMenuStop";
            this.TrayContextMenuStop.Size = new System.Drawing.Size(145, 22);
            this.TrayContextMenuStop.Text = "Stop Server";
            // 
            // TrayContextMenuSeparator1
            // 
            this.TrayContextMenuSeparator1.Name = "TrayContextMenuSeparator1";
            this.TrayContextMenuSeparator1.Size = new System.Drawing.Size(142, 6);
            // 
            // TrayContextMenuClose
            // 
            this.TrayContextMenuClose.Name = "TrayContextMenuClose";
            this.TrayContextMenuClose.Size = new System.Drawing.Size(145, 22);
            this.TrayContextMenuClose.Text = "Close";
            // 
            // ServerRefreshTimer
            // 
            this.ServerRefreshTimer.Enabled = true;
            this.ServerRefreshTimer.Interval = 1000;
            // 
            // UpdateCheckTimer
            // 
            this.UpdateCheckTimer.Enabled = true;
            this.UpdateCheckTimer.Interval = 60000;
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 332);
            this.Controls.Add(this.Tabs);
            this.Controls.Add(this.StatusStrip);
            this.Controls.Add(this.MenuStrip);
            this.MainMenuStrip = this.MenuStrip;
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(500, 371);
            this.Name = "MainWindow";
            this.Text = "ApplicationTitle";
            this.MenuStrip.ResumeLayout(false);
            this.MenuStrip.PerformLayout();
            this.StatusStrip.ResumeLayout(false);
            this.StatusStrip.PerformLayout();
            this.Tabs.ResumeLayout(false);
            this.TabServerControls.ResumeLayout(false);
            this.JoinOptionsGroupBox.ResumeLayout(false);
            this.WorldSelectGroupBox.ResumeLayout(false);
            this.TabAdvancedControls.ResumeLayout(false);
            this.SavingGroupBox.ResumeLayout(false);
            this.TabServerDetails.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.TabPlayers.ResumeLayout(false);
            this.TabLogs.ResumeLayout(false);
            this.TrayContextMenuStrip.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip MenuStrip;
        private System.Windows.Forms.ToolStripMenuItem MenuItemFile;
        private System.Windows.Forms.ToolStripMenuItem MenuItemFileDirectories;
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
        private System.Windows.Forms.GroupBox SavingGroupBox;
        private ValheimServerGUI.Controls.NumericFormField ServerLongBackupIntervalField;
        private ValheimServerGUI.Controls.NumericFormField ServerShortBackupIntervalField;
        private ValheimServerGUI.Controls.NumericFormField ServerBackupsField;
        private ValheimServerGUI.Controls.NumericFormField ServerSaveIntervalField;
        private Controls.TextFormField ServerAdditionalArgsField;
        private System.Windows.Forms.ToolStripMenuItem MenuItemFileSaveProfileAs;
        private System.Windows.Forms.ToolStripMenuItem MenuItemFileOpenSettings;
    }
}