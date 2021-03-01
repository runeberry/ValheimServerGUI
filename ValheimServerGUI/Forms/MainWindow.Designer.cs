
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
            this.StatusStripProgressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.Tabs = new System.Windows.Forms.TabControl();
            this.TabServerControls = new System.Windows.Forms.TabPage();
            this.CheckBoxPublic = new System.Windows.Forms.CheckBox();
            this.LabelPassword = new System.Windows.Forms.Label();
            this.TextBoxPassword = new System.Windows.Forms.TextBox();
            this.LabelServerName = new System.Windows.Forms.Label();
            this.TextBoxServerName = new System.Windows.Forms.TextBox();
            this.LabelWorldSelect = new System.Windows.Forms.Label();
            this.ComboBoxWorldSelect = new System.Windows.Forms.ComboBox();
            this.ButtonStopServer = new System.Windows.Forms.Button();
            this.ButtonStartServer = new System.Windows.Forms.Button();
            this.TabSettings = new System.Windows.Forms.TabPage();
            this.TabLogs = new System.Windows.Forms.TabPage();
            this.ButtonClearLogs = new System.Windows.Forms.Button();
            this.TextBoxLogs = new System.Windows.Forms.TextBox();
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
            this.StatusStripLabel,
            this.StatusStripProgressBar});
            this.StatusStrip.Location = new System.Drawing.Point(0, 264);
            this.StatusStrip.Name = "StatusStrip";
            this.StatusStrip.Size = new System.Drawing.Size(484, 22);
            this.StatusStrip.TabIndex = 1;
            // 
            // StatusStripLabel
            // 
            this.StatusStripLabel.Name = "StatusStripLabel";
            this.StatusStripLabel.Size = new System.Drawing.Size(0, 17);
            // 
            // StatusStripProgressBar
            // 
            this.StatusStripProgressBar.Name = "StatusStripProgressBar";
            this.StatusStripProgressBar.Size = new System.Drawing.Size(100, 16);
            // 
            // Tabs
            // 
            this.Tabs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Tabs.Controls.Add(this.TabServerControls);
            this.Tabs.Controls.Add(this.TabSettings);
            this.Tabs.Controls.Add(this.TabLogs);
            this.Tabs.Location = new System.Drawing.Point(12, 27);
            this.Tabs.Name = "Tabs";
            this.Tabs.SelectedIndex = 0;
            this.Tabs.Size = new System.Drawing.Size(460, 234);
            this.Tabs.TabIndex = 2;
            // 
            // TabServerControls
            // 
            this.TabServerControls.Controls.Add(this.CheckBoxPublic);
            this.TabServerControls.Controls.Add(this.LabelPassword);
            this.TabServerControls.Controls.Add(this.TextBoxPassword);
            this.TabServerControls.Controls.Add(this.LabelServerName);
            this.TabServerControls.Controls.Add(this.TextBoxServerName);
            this.TabServerControls.Controls.Add(this.LabelWorldSelect);
            this.TabServerControls.Controls.Add(this.ComboBoxWorldSelect);
            this.TabServerControls.Controls.Add(this.ButtonStopServer);
            this.TabServerControls.Controls.Add(this.ButtonStartServer);
            this.TabServerControls.Location = new System.Drawing.Point(4, 24);
            this.TabServerControls.Name = "TabServerControls";
            this.TabServerControls.Padding = new System.Windows.Forms.Padding(3);
            this.TabServerControls.Size = new System.Drawing.Size(452, 206);
            this.TabServerControls.TabIndex = 0;
            this.TabServerControls.Text = "Server Controls";
            this.TabServerControls.UseVisualStyleBackColor = true;
            // 
            // CheckBoxPublic
            // 
            this.CheckBoxPublic.AutoSize = true;
            this.CheckBoxPublic.Location = new System.Drawing.Point(3, 141);
            this.CheckBoxPublic.Name = "CheckBoxPublic";
            this.CheckBoxPublic.Size = new System.Drawing.Size(125, 19);
            this.CheckBoxPublic.TabIndex = 9;
            this.CheckBoxPublic.Text = "Community Server";
            this.CheckBoxPublic.UseVisualStyleBackColor = true;
            // 
            // LabelPassword
            // 
            this.LabelPassword.AutoSize = true;
            this.LabelPassword.Location = new System.Drawing.Point(3, 50);
            this.LabelPassword.Name = "LabelPassword";
            this.LabelPassword.Size = new System.Drawing.Size(92, 15);
            this.LabelPassword.TabIndex = 8;
            this.LabelPassword.Text = "Server Password";
            // 
            // TextBoxPassword
            // 
            this.TextBoxPassword.Location = new System.Drawing.Point(3, 68);
            this.TextBoxPassword.Name = "TextBoxPassword";
            this.TextBoxPassword.PasswordChar = '*';
            this.TextBoxPassword.Size = new System.Drawing.Size(236, 23);
            this.TextBoxPassword.TabIndex = 7;
            // 
            // LabelServerName
            // 
            this.LabelServerName.AutoSize = true;
            this.LabelServerName.Location = new System.Drawing.Point(3, 6);
            this.LabelServerName.Name = "LabelServerName";
            this.LabelServerName.Size = new System.Drawing.Size(74, 15);
            this.LabelServerName.TabIndex = 6;
            this.LabelServerName.Text = "Server Name";
            // 
            // TextBoxServerName
            // 
            this.TextBoxServerName.Location = new System.Drawing.Point(3, 24);
            this.TextBoxServerName.Name = "TextBoxServerName";
            this.TextBoxServerName.Size = new System.Drawing.Size(236, 23);
            this.TextBoxServerName.TabIndex = 4;
            // 
            // LabelWorldSelect
            // 
            this.LabelWorldSelect.AutoSize = true;
            this.LabelWorldSelect.Location = new System.Drawing.Point(3, 94);
            this.LabelWorldSelect.Name = "LabelWorldSelect";
            this.LabelWorldSelect.Size = new System.Drawing.Size(39, 15);
            this.LabelWorldSelect.TabIndex = 3;
            this.LabelWorldSelect.Text = "World";
            // 
            // ComboBoxWorldSelect
            // 
            this.ComboBoxWorldSelect.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ComboBoxWorldSelect.FormattingEnabled = true;
            this.ComboBoxWorldSelect.Location = new System.Drawing.Point(3, 112);
            this.ComboBoxWorldSelect.MaxDropDownItems = 100;
            this.ComboBoxWorldSelect.Name = "ComboBoxWorldSelect";
            this.ComboBoxWorldSelect.Size = new System.Drawing.Size(236, 23);
            this.ComboBoxWorldSelect.TabIndex = 2;
            // 
            // ButtonStopServer
            // 
            this.ButtonStopServer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ButtonStopServer.Location = new System.Drawing.Point(84, 180);
            this.ButtonStopServer.Name = "ButtonStopServer";
            this.ButtonStopServer.Size = new System.Drawing.Size(75, 23);
            this.ButtonStopServer.TabIndex = 1;
            this.ButtonStopServer.Text = "Stop Server";
            this.ButtonStopServer.UseVisualStyleBackColor = true;
            this.ButtonStopServer.Click += new System.EventHandler(this.ButtonStopServer_Click);
            // 
            // ButtonStartServer
            // 
            this.ButtonStartServer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ButtonStartServer.Location = new System.Drawing.Point(3, 180);
            this.ButtonStartServer.Name = "ButtonStartServer";
            this.ButtonStartServer.Size = new System.Drawing.Size(75, 23);
            this.ButtonStartServer.TabIndex = 0;
            this.ButtonStartServer.Text = "Start Server";
            this.ButtonStartServer.UseVisualStyleBackColor = true;
            this.ButtonStartServer.Click += new System.EventHandler(this.ButtonStartServer_Click);
            // 
            // TabSettings
            // 
            this.TabSettings.Location = new System.Drawing.Point(4, 24);
            this.TabSettings.Name = "TabSettings";
            this.TabSettings.Padding = new System.Windows.Forms.Padding(3);
            this.TabSettings.Size = new System.Drawing.Size(452, 206);
            this.TabSettings.TabIndex = 1;
            this.TabSettings.Text = "Settings";
            this.TabSettings.UseVisualStyleBackColor = true;
            // 
            // TabLogs
            // 
            this.TabLogs.Controls.Add(this.ButtonClearLogs);
            this.TabLogs.Controls.Add(this.TextBoxLogs);
            this.TabLogs.Location = new System.Drawing.Point(4, 24);
            this.TabLogs.Name = "TabLogs";
            this.TabLogs.Size = new System.Drawing.Size(452, 206);
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
            this.TextBoxLogs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TextBoxLogs.BackColor = System.Drawing.SystemColors.Window;
            this.TextBoxLogs.Font = new System.Drawing.Font("Lucida Console", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.TextBoxLogs.Location = new System.Drawing.Point(3, 32);
            this.TextBoxLogs.Multiline = true;
            this.TextBoxLogs.Name = "TextBoxLogs";
            this.TextBoxLogs.ReadOnly = true;
            this.TextBoxLogs.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.TextBoxLogs.Size = new System.Drawing.Size(446, 171);
            this.TextBoxLogs.TabIndex = 0;
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 286);
            this.Controls.Add(this.Tabs);
            this.Controls.Add(this.StatusStrip);
            this.Controls.Add(this.MenuStrip);
            this.MainMenuStrip = this.MenuStrip;
            this.MinimumSize = new System.Drawing.Size(500, 325);
            this.Name = "MainWindow";
            this.Text = "(Unofficial) Valheim Dedicated Server GUI";
            this.MenuStrip.ResumeLayout(false);
            this.MenuStrip.PerformLayout();
            this.StatusStrip.ResumeLayout(false);
            this.StatusStrip.PerformLayout();
            this.Tabs.ResumeLayout(false);
            this.TabServerControls.ResumeLayout(false);
            this.TabServerControls.PerformLayout();
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
        private System.Windows.Forms.ToolStripProgressBar StatusStripProgressBar;
        private System.Windows.Forms.TabControl Tabs;
        private System.Windows.Forms.TabPage TabServerControls;
        private System.Windows.Forms.TabPage TabSettings;
        private System.Windows.Forms.TabPage TabLogs;
        private System.Windows.Forms.Button ButtonStopServer;
        private System.Windows.Forms.Button ButtonStartServer;
        private System.Windows.Forms.Label LabelWorldSelect;
        private System.Windows.Forms.ComboBox ComboBoxWorldSelect;
        private System.Windows.Forms.Label LabelServerName;
        private System.Windows.Forms.TextBox TextBoxServerName;
        private System.Windows.Forms.Label LabelPassword;
        private System.Windows.Forms.TextBox TextBoxPassword;
        private System.Windows.Forms.CheckBox CheckBoxPublic;
        private System.Windows.Forms.TextBox TextBoxLogs;
        private System.Windows.Forms.Button ButtonClearLogs;
    }
}