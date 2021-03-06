
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
            this.MenuItemHelpUpdates = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemHelpSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.MenuItemHelpAbout = new System.Windows.Forms.ToolStripMenuItem();
            this.StatusStrip = new System.Windows.Forms.StatusStrip();
            this.StatusStripLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.Tabs = new System.Windows.Forms.TabControl();
            this.TabServerControls = new System.Windows.Forms.TabPage();
            this.ServerPortField = new ValheimServerGUI.Controls.NumericFormField();
            this.ButtonRestartServer = new System.Windows.Forms.Button();
            this.ShowPasswordField = new ValheimServerGUI.Controls.CheckboxFormField();
            this.CommunityServerField = new ValheimServerGUI.Controls.CheckboxFormField();
            this.WorldSelectField = new ValheimServerGUI.Controls.DropdownFormField();
            this.ServerPasswordField = new ValheimServerGUI.Forms.Controls.TextFormField();
            this.ServerNameField = new ValheimServerGUI.Forms.Controls.TextFormField();
            this.ButtonStopServer = new System.Windows.Forms.Button();
            this.ButtonStartServer = new System.Windows.Forms.Button();
            this.TabLogs = new System.Windows.Forms.TabPage();
            this.ButtonClearLogs = new System.Windows.Forms.Button();
            this.TextBoxLogs = new System.Windows.Forms.TextBox();
            this.NotifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.MenuStrip.SuspendLayout();
            this.StatusStrip.SuspendLayout();
            this.Tabs.SuspendLayout();
            this.TabServerControls.SuspendLayout();
            this.TabLogs.SuspendLayout();
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
            this.MenuItemHelpUpdates,
            this.MenuItemHelpSeparator1,
            this.MenuItemHelpAbout});
            this.MenuItemHelp.Name = "MenuItemHelp";
            this.MenuItemHelp.Size = new System.Drawing.Size(44, 20);
            this.MenuItemHelp.Text = "&Help";
            // 
            // MenuItemHelpUpdates
            // 
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
            this.StatusStrip.Location = new System.Drawing.Point(0, 289);
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
            this.Tabs.Controls.Add(this.TabLogs);
            this.Tabs.Location = new System.Drawing.Point(12, 27);
            this.Tabs.Name = "Tabs";
            this.Tabs.SelectedIndex = 0;
            this.Tabs.Size = new System.Drawing.Size(460, 259);
            this.Tabs.TabIndex = 2;
            // 
            // TabServerControls
            // 
            this.TabServerControls.Controls.Add(this.ServerPortField);
            this.TabServerControls.Controls.Add(this.ButtonRestartServer);
            this.TabServerControls.Controls.Add(this.ShowPasswordField);
            this.TabServerControls.Controls.Add(this.CommunityServerField);
            this.TabServerControls.Controls.Add(this.WorldSelectField);
            this.TabServerControls.Controls.Add(this.ServerPasswordField);
            this.TabServerControls.Controls.Add(this.ServerNameField);
            this.TabServerControls.Controls.Add(this.ButtonStopServer);
            this.TabServerControls.Controls.Add(this.ButtonStartServer);
            this.TabServerControls.Location = new System.Drawing.Point(4, 24);
            this.TabServerControls.Name = "TabServerControls";
            this.TabServerControls.Padding = new System.Windows.Forms.Padding(3);
            this.TabServerControls.Size = new System.Drawing.Size(452, 231);
            this.TabServerControls.TabIndex = 0;
            this.TabServerControls.Text = "Server Controls";
            this.TabServerControls.UseVisualStyleBackColor = true;
            // 
            // ServerPortField
            // 
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
            this.ButtonRestartServer.Location = new System.Drawing.Point(104, 205);
            this.ButtonRestartServer.Name = "ButtonRestartServer";
            this.ButtonRestartServer.Size = new System.Drawing.Size(95, 23);
            this.ButtonRestartServer.TabIndex = 15;
            this.ButtonRestartServer.Text = "Restart Server";
            this.ButtonRestartServer.UseVisualStyleBackColor = true;
            this.ButtonRestartServer.Click += new System.EventHandler(this.ButtonRestartServer_Click);
            // 
            // ShowPasswordField
            // 
            this.ShowPasswordField.LabelText = "Show Password";
            this.ShowPasswordField.Location = new System.Drawing.Point(234, 68);
            this.ShowPasswordField.Name = "ShowPasswordField";
            this.ShowPasswordField.Size = new System.Drawing.Size(150, 17);
            this.ShowPasswordField.TabIndex = 14;
            this.ShowPasswordField.Value = false;
            // 
            // CommunityServerField
            // 
            this.CommunityServerField.LabelText = "Community Server";
            this.CommunityServerField.Location = new System.Drawing.Point(0, 141);
            this.CommunityServerField.Name = "CommunityServerField";
            this.CommunityServerField.Size = new System.Drawing.Size(150, 17);
            this.CommunityServerField.TabIndex = 13;
            this.CommunityServerField.Value = false;
            // 
            // WorldSelectField
            // 
            this.WorldSelectField.DataSource = ((System.Collections.Generic.IEnumerable<string>)(resources.GetObject("WorldSelectField.DataSource")));
            this.WorldSelectField.DropdownEnabled = true;
            this.WorldSelectField.EmptyText = "(no worlds)";
            this.WorldSelectField.LabelText = "World";
            this.WorldSelectField.Location = new System.Drawing.Point(0, 94);
            this.WorldSelectField.Name = "WorldSelectField";
            this.WorldSelectField.Size = new System.Drawing.Size(243, 41);
            this.WorldSelectField.TabIndex = 12;
            this.WorldSelectField.Value = null;
            // 
            // ServerPasswordField
            // 
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
            this.ButtonStopServer.Location = new System.Drawing.Point(205, 205);
            this.ButtonStopServer.Name = "ButtonStopServer";
            this.ButtonStopServer.Size = new System.Drawing.Size(95, 23);
            this.ButtonStopServer.TabIndex = 1;
            this.ButtonStopServer.Text = "Stop Server";
            this.ButtonStopServer.UseVisualStyleBackColor = true;
            this.ButtonStopServer.Click += new System.EventHandler(this.ButtonStopServer_Click);
            // 
            // ButtonStartServer
            // 
            this.ButtonStartServer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ButtonStartServer.Location = new System.Drawing.Point(3, 205);
            this.ButtonStartServer.Name = "ButtonStartServer";
            this.ButtonStartServer.Size = new System.Drawing.Size(95, 23);
            this.ButtonStartServer.TabIndex = 0;
            this.ButtonStartServer.Text = "Start Server";
            this.ButtonStartServer.UseVisualStyleBackColor = true;
            this.ButtonStartServer.Click += new System.EventHandler(this.ButtonStartServer_Click);
            // 
            // TabLogs
            // 
            this.TabLogs.Controls.Add(this.ButtonClearLogs);
            this.TabLogs.Controls.Add(this.TextBoxLogs);
            this.TabLogs.Location = new System.Drawing.Point(4, 24);
            this.TabLogs.Name = "TabLogs";
            this.TabLogs.Size = new System.Drawing.Size(452, 231);
            this.TabLogs.TabIndex = 2;
            this.TabLogs.Text = "Logs";
            this.TabLogs.UseVisualStyleBackColor = true;
            // 
            // ButtonClearLogs
            // 
            this.ButtonClearLogs.Location = new System.Drawing.Point(3, 3);
            this.ButtonClearLogs.Name = "ButtonClearLogs";
            this.ButtonClearLogs.Size = new System.Drawing.Size(75, 23);
            this.ButtonClearLogs.TabIndex = 1;
            this.ButtonClearLogs.Text = "Clear Logs";
            this.ButtonClearLogs.UseVisualStyleBackColor = true;
            this.ButtonClearLogs.Click += new System.EventHandler(this.ButtonClearLogs_Click);
            // 
            // TextBoxLogs
            // 
            this.TextBoxLogs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TextBoxLogs.BackColor = System.Drawing.SystemColors.Window;
            this.TextBoxLogs.Font = new System.Drawing.Font("Lucida Console", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.TextBoxLogs.Location = new System.Drawing.Point(3, 32);
            this.TextBoxLogs.Multiline = true;
            this.TextBoxLogs.Name = "TextBoxLogs";
            this.TextBoxLogs.ReadOnly = true;
            this.TextBoxLogs.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.TextBoxLogs.Size = new System.Drawing.Size(446, 196);
            this.TextBoxLogs.TabIndex = 0;
            // 
            // NotifyIcon
            // 
            this.NotifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("NotifyIcon.Icon")));
            this.NotifyIcon.Text = "ValheimServerGUI";
            this.NotifyIcon.Visible = true;
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 311);
            this.Controls.Add(this.Tabs);
            this.Controls.Add(this.StatusStrip);
            this.Controls.Add(this.MenuStrip);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.MenuStrip;
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(500, 325);
            this.Name = "MainWindow";
            this.Text = "(Unofficial) Valheim Dedicated Server GUI";
            this.MenuStrip.ResumeLayout(false);
            this.MenuStrip.PerformLayout();
            this.StatusStrip.ResumeLayout(false);
            this.StatusStrip.PerformLayout();
            this.Tabs.ResumeLayout(false);
            this.TabServerControls.ResumeLayout(false);
            this.TabLogs.ResumeLayout(false);
            this.TabLogs.PerformLayout();
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
        private System.Windows.Forms.TextBox TextBoxLogs;
        private System.Windows.Forms.Button ButtonClearLogs;
        private ValheimServerGUI.Controls.DropdownFormField WorldSelectField;
        private Controls.TextFormField ServerPasswordField;
        private Controls.TextFormField ServerNameField;
        private ValheimServerGUI.Controls.CheckboxFormField CommunityServerField;
        private ValheimServerGUI.Controls.CheckboxFormField ShowPasswordField;
        private System.Windows.Forms.Button ButtonRestartServer;
        private ValheimServerGUI.Controls.NumericFormField ServerPortField;
        private System.Windows.Forms.NotifyIcon NotifyIcon;
    }
}