
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
            this.MenuItemFileDirectories = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemFileSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.MenuItemFileClose = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemHelp = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemHelpManual = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemHelpUpdates = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemHelpSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.MenuItemHelpAbout = new System.Windows.Forms.ToolStripMenuItem();
            this.StatusStrip = new System.Windows.Forms.StatusStrip();
            this.StatusStripLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.Tabs = new System.Windows.Forms.TabControl();
            this.TabServerControls = new System.Windows.Forms.TabPage();
            this.WorldSelectGroupBox = new System.Windows.Forms.GroupBox();
            this.WorldSelectNewNameField = new ValheimServerGUI.Forms.Controls.TextFormField();
            this.WorldSelectRadioNew = new ValheimServerGUI.Controls.RadioFormField();
            this.WorldSelectRadioExisting = new ValheimServerGUI.Controls.RadioFormField();
            this.WorldSelectExistingNameField = new ValheimServerGUI.Controls.DropdownFormField();
            this.ServerPortField = new ValheimServerGUI.Controls.NumericFormField();
            this.ButtonRestartServer = new System.Windows.Forms.Button();
            this.ShowPasswordField = new ValheimServerGUI.Controls.CheckboxFormField();
            this.CommunityServerField = new ValheimServerGUI.Controls.CheckboxFormField();
            this.ServerPasswordField = new ValheimServerGUI.Forms.Controls.TextFormField();
            this.ServerNameField = new ValheimServerGUI.Forms.Controls.TextFormField();
            this.ButtonStopServer = new System.Windows.Forms.Button();
            this.ButtonStartServer = new System.Windows.Forms.Button();
            this.TabPlayers = new System.Windows.Forms.TabPage();
            this.PlayersTable = new ValheimServerGUI.Controls.DataListView();
            this.ColumnPlayerStatus = new System.Windows.Forms.ColumnHeader();
            this.ColumnPlayerName = new System.Windows.Forms.ColumnHeader();
            this.ColumnPlayerUpdated = new System.Windows.Forms.ColumnHeader();
            this.ImageList = new System.Windows.Forms.ImageList(this.components);
            this.TabLogs = new System.Windows.Forms.TabPage();
            this.LogViewSelectField = new ValheimServerGUI.Controls.DropdownFormField();
            this.LogViewer = new ValheimServerGUI.Controls.LogViewer();
            this.ButtonClearLogs = new System.Windows.Forms.Button();
            this.NotifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.TrayContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.TrayContextMenuStart = new System.Windows.Forms.ToolStripMenuItem();
            this.TrayContextMenuRestart = new System.Windows.Forms.ToolStripMenuItem();
            this.TrayContextMenuStop = new System.Windows.Forms.ToolStripMenuItem();
            this.TrayContextMenuSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.TrayContextMenuClose = new System.Windows.Forms.ToolStripMenuItem();
            this.ServerRefreshTimer = new System.Windows.Forms.Timer(this.components);
            this.ButtonSaveLogs = new System.Windows.Forms.Button();
            this.MenuStrip.SuspendLayout();
            this.StatusStrip.SuspendLayout();
            this.Tabs.SuspendLayout();
            this.TabServerControls.SuspendLayout();
            this.WorldSelectGroupBox.SuspendLayout();
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
            this.MenuItemFileDirectories,
            this.MenuItemFileSeparator1,
            this.MenuItemFileClose});
            this.MenuItemFile.Name = "MenuItemFile";
            this.MenuItemFile.Size = new System.Drawing.Size(37, 20);
            this.MenuItemFile.Text = "&File";
            // 
            // MenuItemFileDirectories
            // 
            this.MenuItemFileDirectories.Image = ((System.Drawing.Image)(resources.GetObject("MenuItemFileDirectories.Image")));
            this.MenuItemFileDirectories.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.MenuItemFileDirectories.Name = "MenuItemFileDirectories";
            this.MenuItemFileDirectories.Size = new System.Drawing.Size(158, 22);
            this.MenuItemFileDirectories.Text = "Set &Directories...";
            // 
            // MenuItemFileSeparator1
            // 
            this.MenuItemFileSeparator1.Name = "MenuItemFileSeparator1";
            this.MenuItemFileSeparator1.Size = new System.Drawing.Size(155, 6);
            // 
            // MenuItemFileClose
            // 
            this.MenuItemFileClose.Name = "MenuItemFileClose";
            this.MenuItemFileClose.Size = new System.Drawing.Size(158, 22);
            this.MenuItemFileClose.Text = "&Close";
            // 
            // MenuItemHelp
            // 
            this.MenuItemHelp.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItemHelpManual,
            this.MenuItemHelpUpdates,
            this.MenuItemHelpSeparator1,
            this.MenuItemHelpAbout});
            this.MenuItemHelp.Name = "MenuItemHelp";
            this.MenuItemHelp.Size = new System.Drawing.Size(44, 20);
            this.MenuItemHelp.Text = "&Help";
            // 
            // MenuItemHelpManual
            // 
            this.MenuItemHelpManual.Image = ((System.Drawing.Image)(resources.GetObject("MenuItemHelpManual.Image")));
            this.MenuItemHelpManual.Name = "MenuItemHelpManual";
            this.MenuItemHelpManual.Size = new System.Drawing.Size(171, 22);
            this.MenuItemHelpManual.Text = "Online &Manual";
            // 
            // MenuItemHelpUpdates
            // 
            this.MenuItemHelpUpdates.Image = ((System.Drawing.Image)(resources.GetObject("MenuItemHelpUpdates.Image")));
            this.MenuItemHelpUpdates.Name = "MenuItemHelpUpdates";
            this.MenuItemHelpUpdates.Size = new System.Drawing.Size(171, 22);
            this.MenuItemHelpUpdates.Text = "Check for &Updates";
            // 
            // MenuItemHelpSeparator1
            // 
            this.MenuItemHelpSeparator1.Name = "MenuItemHelpSeparator1";
            this.MenuItemHelpSeparator1.Size = new System.Drawing.Size(168, 6);
            // 
            // MenuItemHelpAbout
            // 
            this.MenuItemHelpAbout.Name = "MenuItemHelpAbout";
            this.MenuItemHelpAbout.Size = new System.Drawing.Size(171, 22);
            this.MenuItemHelpAbout.Text = "&About...";
            // 
            // StatusStrip
            // 
            this.StatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.StatusStripLabel});
            this.StatusStrip.Location = new System.Drawing.Point(0, 310);
            this.StatusStrip.Name = "StatusStrip";
            this.StatusStrip.Size = new System.Drawing.Size(484, 22);
            this.StatusStrip.TabIndex = 1;
            // 
            // StatusStripLabel
            // 
            this.StatusStripLabel.Name = "StatusStripLabel";
            this.StatusStripLabel.Size = new System.Drawing.Size(0, 17);
            // 
            // Tabs
            // 
            this.Tabs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Tabs.Controls.Add(this.TabServerControls);
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
            this.TabServerControls.Controls.Add(this.WorldSelectGroupBox);
            this.TabServerControls.Controls.Add(this.ServerPortField);
            this.TabServerControls.Controls.Add(this.ButtonRestartServer);
            this.TabServerControls.Controls.Add(this.ShowPasswordField);
            this.TabServerControls.Controls.Add(this.CommunityServerField);
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
            // WorldSelectGroupBox
            // 
            this.WorldSelectGroupBox.Controls.Add(this.WorldSelectNewNameField);
            this.WorldSelectGroupBox.Controls.Add(this.WorldSelectRadioNew);
            this.WorldSelectGroupBox.Controls.Add(this.WorldSelectRadioExisting);
            this.WorldSelectGroupBox.Controls.Add(this.WorldSelectExistingNameField);
            this.WorldSelectGroupBox.Location = new System.Drawing.Point(3, 117);
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
            this.WorldSelectNewNameField.MaxLength = 32767;
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
            this.ShowPasswordField.Location = new System.Drawing.Point(234, 68);
            this.ShowPasswordField.Name = "ShowPasswordField";
            this.ShowPasswordField.Size = new System.Drawing.Size(150, 17);
            this.ShowPasswordField.TabIndex = 14;
            this.ShowPasswordField.Value = false;
            // 
            // CommunityServerField
            // 
            this.CommunityServerField.HelpText = resources.GetString("CommunityServerField.HelpText");
            this.CommunityServerField.LabelText = "Community Server";
            this.CommunityServerField.Location = new System.Drawing.Point(3, 94);
            this.CommunityServerField.Name = "CommunityServerField";
            this.CommunityServerField.Size = new System.Drawing.Size(142, 17);
            this.CommunityServerField.TabIndex = 13;
            this.CommunityServerField.Value = false;
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
            // TabPlayers
            // 
            this.TabPlayers.Controls.Add(this.PlayersTable);
            this.TabPlayers.Location = new System.Drawing.Point(4, 24);
            this.TabPlayers.Name = "TabPlayers";
            this.TabPlayers.Size = new System.Drawing.Size(452, 252);
            this.TabPlayers.TabIndex = 3;
            this.TabPlayers.Text = "Players";
            this.TabPlayers.UseVisualStyleBackColor = true;
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
            this.ColumnPlayerName.Text = "Name";
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
            // LogViewSelectField
            // 
            this.LogViewSelectField.DataSource = ((System.Collections.Generic.IEnumerable<string>)(resources.GetObject("LogViewSelectField.DataSource")));
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
            this.LogViewer.Location = new System.Drawing.Point(3, 51);
            this.LogViewer.LogView = "DefaultLogView";
            this.LogViewer.Name = "LogViewer";
            this.LogViewer.Size = new System.Drawing.Size(446, 198);
            this.LogViewer.TabIndex = 2;
            this.LogViewer.TimestampFormat = "T";
            // 
            // ButtonClearLogs
            // 
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
            this.TrayContextMenuStart,
            this.TrayContextMenuRestart,
            this.TrayContextMenuStop,
            this.TrayContextMenuSeparator1,
            this.TrayContextMenuClose});
            this.TrayContextMenuStrip.Name = "TrayContextMenuStrip";
            this.TrayContextMenuStrip.Size = new System.Drawing.Size(146, 98);
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
            this.ServerRefreshTimer.Interval = 1000;
            // 
            // ButtonSaveLogs
            // 
            this.ButtonSaveLogs.Location = new System.Drawing.Point(280, 22);
            this.ButtonSaveLogs.Name = "ButtonSaveLogs";
            this.ButtonSaveLogs.Size = new System.Drawing.Size(88, 23);
            this.ButtonSaveLogs.TabIndex = 4;
            this.ButtonSaveLogs.Text = "Save Logs...";
            this.ButtonSaveLogs.UseVisualStyleBackColor = true;
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 332);
            this.Controls.Add(this.Tabs);
            this.Controls.Add(this.StatusStrip);
            this.Controls.Add(this.MenuStrip);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.MenuStrip;
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(500, 371);
            this.Name = "MainWindow";
            this.Text = "(Unofficial) Valheim Dedicated Server GUI";
            this.MenuStrip.ResumeLayout(false);
            this.MenuStrip.PerformLayout();
            this.StatusStrip.ResumeLayout(false);
            this.StatusStrip.PerformLayout();
            this.Tabs.ResumeLayout(false);
            this.TabServerControls.ResumeLayout(false);
            this.WorldSelectGroupBox.ResumeLayout(false);
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
        private System.Windows.Forms.ToolStripStatusLabel StatusStripLabel;
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
    }
}