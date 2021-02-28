
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ValheimServerGUI
{
    partial class MainWindow
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
            this.MainWindowStatusStrip = new System.Windows.Forms.StatusStrip();
            this.StatusStripLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.StatusStripProgressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.MainWindowMenuStrip = new System.Windows.Forms.MenuStrip();
            this.MenuItemFile = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemFileDirectories = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemFileSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.MenuItemFileExit = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemHelp = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemHelpUpdates = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemHelpSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.MenuItemHelpAbout = new System.Windows.Forms.ToolStripMenuItem();
            this.MainWindowStatusStrip.SuspendLayout();
            this.MainWindowMenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // MainWindowStatusStrip
            // 
            this.MainWindowStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.StatusStripLabel,
            this.StatusStripProgressBar});
            this.MainWindowStatusStrip.Location = new System.Drawing.Point(0, 202);
            this.MainWindowStatusStrip.Name = "MainWindowStatusStrip";
            this.MainWindowStatusStrip.Size = new System.Drawing.Size(457, 22);
            this.MainWindowStatusStrip.TabIndex = 1;
            // 
            // StatusStripLabel
            // 
            this.StatusStripLabel.Name = "StatusStripLabel";
            this.StatusStripLabel.Size = new System.Drawing.Size(51, 17);
            this.StatusStripLabel.Text = "Stopped";
            // 
            // StatusStripProgressBar
            // 
            this.StatusStripProgressBar.Name = "StatusStripProgressBar";
            this.StatusStripProgressBar.Size = new System.Drawing.Size(100, 16);
            // 
            // MainWindowMenuStrip
            // 
            this.MainWindowMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItemFile,
            this.MenuItemHelp});
            this.MainWindowMenuStrip.Location = new System.Drawing.Point(0, 0);
            this.MainWindowMenuStrip.Name = "MainWindowMenuStrip";
            this.MainWindowMenuStrip.Size = new System.Drawing.Size(457, 24);
            this.MainWindowMenuStrip.TabIndex = 2;
            // 
            // MenuItemFile
            // 
            this.MenuItemFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItemFileDirectories,
            this.MenuItemFileSeparator1,
            this.MenuItemFileExit});
            this.MenuItemFile.Name = "MenuItemFile";
            this.MenuItemFile.Size = new System.Drawing.Size(37, 20);
            this.MenuItemFile.Text = "&File";
            // 
            // MenuItemFileDirectories
            // 
            this.MenuItemFileDirectories.Image = ((System.Drawing.Image)(resources.GetObject("MenuItemFileDirectories.Image")));
            this.MenuItemFileDirectories.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.MenuItemFileDirectories.Name = "MenuItemFileDirectories";
            this.MenuItemFileDirectories.Size = new System.Drawing.Size(192, 22);
            this.MenuItemFileDirectories.Text = "Set &Game Directories...";
            // 
            // MenuItemFileSeparator1
            // 
            this.MenuItemFileSeparator1.Name = "MenuItemFileSeparator1";
            this.MenuItemFileSeparator1.Size = new System.Drawing.Size(189, 6);
            // 
            // MenuItemFileExit
            // 
            this.MenuItemFileExit.Name = "MenuItemFileExit";
            this.MenuItemFileExit.Size = new System.Drawing.Size(192, 22);
            this.MenuItemFileExit.Text = "&Close";
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
            this.MenuItemHelpUpdates.Text = "&Check for Updates";
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
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(457, 224);
            this.Controls.Add(this.MainWindowStatusStrip);
            this.Controls.Add(this.MainWindowMenuStrip);
            this.Name = "MainWindow";
            this.Text = "(Unofficial) Valheim Dedicated Server GUI";
            this.MainWindowStatusStrip.ResumeLayout(false);
            this.MainWindowStatusStrip.PerformLayout();
            this.MainWindowMenuStrip.ResumeLayout(false);
            this.MainWindowMenuStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private StatusStrip MainWindowStatusStrip;
        private MenuStrip MainWindowMenuStrip;
        private ToolStripSeparator MenuItemFileSeparator1;
        private ToolStripSeparator MenuItemHelpSeparator1;
        private ToolStripMenuItem MenuItemFile;
        private ToolStripMenuItem MenuItemFileDirectories;
        private ToolStripMenuItem MenuItemFileExit;
        private ToolStripMenuItem MenuItemHelp;
        private ToolStripMenuItem MenuItemHelpUpdates;
        private ToolStripMenuItem MenuItemHelpAbout;
        private ToolStripStatusLabel StatusStripLabel;
        private ToolStripProgressBar StatusStripProgressBar;
    }
}

