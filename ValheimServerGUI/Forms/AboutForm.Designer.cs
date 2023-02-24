
namespace ValheimServerGUI.Forms
{
    partial class AboutForm
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
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.VersionLabel = new System.Windows.Forms.Label();
            this.ButtonGitHub = new System.Windows.Forms.Button();
            this.ButtonDonate = new System.Windows.Forms.Button();
            this.ButtonValheimSite = new System.Windows.Forms.Button();
            this.ButtonDiscord = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::ValheimServerGUI.Properties.Resources.RuneberryLogo;
            this.pictureBox1.Location = new System.Drawing.Point(12, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(100, 100);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(118, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(253, 65);
            this.label1.TabIndex = 1;
            this.label1.Text = "Valheim Dedicated Server GUI\r\n\r\n© 2023 Runeberry Software, LLC\r\nLicensed under GN" +
    "U GPLv3";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point);
            this.label2.Location = new System.Drawing.Point(12, 115);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(359, 61);
            this.label2.TabIndex = 3;
            this.label2.Text = "This is a fan-made project. Runeberry Software is not affiliated with Valheim or " +
    "Iron Gate Studio. We are not responsible for the loss of any save data. Use at y" +
    "our own risk!";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // VersionLabel
            // 
            this.VersionLabel.Location = new System.Drawing.Point(119, 77);
            this.VersionLabel.Name = "VersionLabel";
            this.VersionLabel.Size = new System.Drawing.Size(253, 35);
            this.VersionLabel.TabIndex = 2;
            this.VersionLabel.Text = "Version: 0.0.0-rc.1\r\nBuild Date: 2000-01-01T00:00:00Z";
            this.VersionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ButtonGitHub
            // 
            this.ButtonGitHub.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonGitHub.Image = global::ValheimServerGUI.Properties.Resources.GitHubLogo;
            this.ButtonGitHub.Location = new System.Drawing.Point(12, 216);
            this.ButtonGitHub.Name = "ButtonGitHub";
            this.ButtonGitHub.Size = new System.Drawing.Size(116, 23);
            this.ButtonGitHub.TabIndex = 5;
            this.ButtonGitHub.Text = "GitHub";
            this.ButtonGitHub.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.ButtonGitHub.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.ButtonGitHub.UseVisualStyleBackColor = true;
            this.ButtonGitHub.Click += new System.EventHandler(this.ButtonGitHub_Click);
            // 
            // ButtonDonate
            // 
            this.ButtonDonate.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonDonate.Image = global::ValheimServerGUI.Properties.Resources.DonateLogo;
            this.ButtonDonate.Location = new System.Drawing.Point(256, 216);
            this.ButtonDonate.Name = "ButtonDonate";
            this.ButtonDonate.Size = new System.Drawing.Size(116, 23);
            this.ButtonDonate.TabIndex = 7;
            this.ButtonDonate.Text = "Donate";
            this.ButtonDonate.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.ButtonDonate.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.ButtonDonate.UseVisualStyleBackColor = true;
            this.ButtonDonate.Click += new System.EventHandler(this.ButtonDonate_Click);
            // 
            // ButtonValheimSite
            // 
            this.ButtonValheimSite.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonValheimSite.Image = global::ValheimServerGUI.Properties.Resources.vsg_logo_16;
            this.ButtonValheimSite.Location = new System.Drawing.Point(12, 187);
            this.ButtonValheimSite.Name = "ButtonValheimSite";
            this.ButtonValheimSite.Size = new System.Drawing.Size(360, 23);
            this.ButtonValheimSite.TabIndex = 4;
            this.ButtonValheimSite.Text = "Don\'t have Valheim yet? Why not? Buy it here!";
            this.ButtonValheimSite.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.ButtonValheimSite.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.ButtonValheimSite.UseVisualStyleBackColor = true;
            this.ButtonValheimSite.Click += new System.EventHandler(this.ButtonValheimSite_Click);
            // 
            // ButtonDiscord
            // 
            this.ButtonDiscord.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonDiscord.Image = global::ValheimServerGUI.Properties.Resources.DiscordLogo;
            this.ButtonDiscord.Location = new System.Drawing.Point(134, 216);
            this.ButtonDiscord.Name = "ButtonDiscord";
            this.ButtonDiscord.Size = new System.Drawing.Size(116, 23);
            this.ButtonDiscord.TabIndex = 6;
            this.ButtonDiscord.Text = "Discord";
            this.ButtonDiscord.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.ButtonDiscord.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.ButtonDiscord.UseVisualStyleBackColor = true;
            this.ButtonDiscord.Click += new System.EventHandler(this.ButtonDiscord_Click);
            // 
            // AboutForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 251);
            this.Controls.Add(this.ButtonDiscord);
            this.Controls.Add(this.ButtonValheimSite);
            this.Controls.Add(this.ButtonDonate);
            this.Controls.Add(this.ButtonGitHub);
            this.Controls.Add(this.VersionLabel);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pictureBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AboutForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "About";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label VersionLabel;
        private System.Windows.Forms.Button ButtonGitHub;
        private System.Windows.Forms.Button ButtonDonate;
        private System.Windows.Forms.Button ButtonValheimSite;
        private System.Windows.Forms.Button ButtonDiscord;
    }
}