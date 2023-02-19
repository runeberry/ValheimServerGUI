
namespace ValheimServerGUI.Forms
{
    partial class PlayerDetailsForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PlayerDetailsForm));
            this.PlayerNameField = new ValheimServerGUI.Controls.LabelField();
            this.ButtonOK = new System.Windows.Forms.Button();
            this.ButtonRefresh = new System.Windows.Forms.Button();
            this.SteamIdField = new ValheimServerGUI.Controls.LabelField();
            this.ZdoIdField = new ValheimServerGUI.Controls.LabelField();
            this.OnlineStatusField = new ValheimServerGUI.Controls.LabelField();
            this.StatusChangedField = new ValheimServerGUI.Controls.LabelField();
            this.SteamIdWarningIcon = new System.Windows.Forms.PictureBox();
            this.SteamIdCopyButton = new ValheimServerGUI.Forms.CopyButton();
            ((System.ComponentModel.ISupportInitialize)(this.SteamIdWarningIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // PlayerNameField
            // 
            this.PlayerNameField.HelpText = resources.GetString("PlayerNameField.HelpText");
            this.PlayerNameField.LabelSplitRatio = 0.5D;
            this.PlayerNameField.LabelText = "Character Name:";
            this.PlayerNameField.LabelTextAlign = System.Drawing.ContentAlignment.TopRight;
            this.PlayerNameField.Location = new System.Drawing.Point(12, 12);
            this.PlayerNameField.Name = "PlayerNameField";
            this.PlayerNameField.Size = new System.Drawing.Size(240, 15);
            this.PlayerNameField.TabIndex = 0;
            this.PlayerNameField.Value = "";
            this.PlayerNameField.ValueTextAlign = System.Drawing.ContentAlignment.TopLeft;
            // 
            // ButtonOK
            // 
            this.ButtonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonOK.Location = new System.Drawing.Point(202, 120);
            this.ButtonOK.Name = "ButtonOK";
            this.ButtonOK.Size = new System.Drawing.Size(75, 23);
            this.ButtonOK.TabIndex = 7;
            this.ButtonOK.Text = "OK";
            this.ButtonOK.UseVisualStyleBackColor = true;
            // 
            // ButtonRefresh
            // 
            this.ButtonRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonRefresh.Image = global::ValheimServerGUI.Properties.Resources.Restart_16x;
            this.ButtonRefresh.Location = new System.Drawing.Point(173, 120);
            this.ButtonRefresh.Name = "ButtonRefresh";
            this.ButtonRefresh.Size = new System.Drawing.Size(23, 23);
            this.ButtonRefresh.TabIndex = 6;
            this.ButtonRefresh.UseVisualStyleBackColor = true;
            // 
            // SteamIdField
            // 
            this.SteamIdField.HelpText = "The ID associated with the player\'s Steam account. You can use this ID to ban or " +
    "block players, or add players as admins.";
            this.SteamIdField.LabelSplitRatio = 0.5D;
            this.SteamIdField.LabelText = "Steam ID:";
            this.SteamIdField.LabelTextAlign = System.Drawing.ContentAlignment.TopRight;
            this.SteamIdField.Location = new System.Drawing.Point(12, 33);
            this.SteamIdField.Name = "SteamIdField";
            this.SteamIdField.Size = new System.Drawing.Size(240, 15);
            this.SteamIdField.TabIndex = 1;
            this.SteamIdField.Value = "";
            this.SteamIdField.ValueTextAlign = System.Drawing.ContentAlignment.TopLeft;
            // 
            // ZdoIdField
            // 
            this.ZdoIdField.HelpText = "The player\'s object ID in game. This changes with each game session.";
            this.ZdoIdField.LabelSplitRatio = 0.5D;
            this.ZdoIdField.LabelText = "ZDOID:";
            this.ZdoIdField.LabelTextAlign = System.Drawing.ContentAlignment.TopRight;
            this.ZdoIdField.Location = new System.Drawing.Point(12, 54);
            this.ZdoIdField.Name = "ZdoIdField";
            this.ZdoIdField.Size = new System.Drawing.Size(240, 15);
            this.ZdoIdField.TabIndex = 3;
            this.ZdoIdField.Value = "";
            this.ZdoIdField.ValueTextAlign = System.Drawing.ContentAlignment.TopLeft;
            // 
            // OnlineStatusField
            // 
            this.OnlineStatusField.HelpText = "Possible statuses are: Online, Offline, Joining, or Leaving";
            this.OnlineStatusField.LabelSplitRatio = 0.5D;
            this.OnlineStatusField.LabelText = "Online Status:";
            this.OnlineStatusField.LabelTextAlign = System.Drawing.ContentAlignment.TopRight;
            this.OnlineStatusField.Location = new System.Drawing.Point(12, 75);
            this.OnlineStatusField.Name = "OnlineStatusField";
            this.OnlineStatusField.Size = new System.Drawing.Size(240, 15);
            this.OnlineStatusField.TabIndex = 4;
            this.OnlineStatusField.Value = "";
            this.OnlineStatusField.ValueTextAlign = System.Drawing.ContentAlignment.TopLeft;
            // 
            // StatusChangedField
            // 
            this.StatusChangedField.HelpText = "";
            this.StatusChangedField.LabelSplitRatio = 0.5D;
            this.StatusChangedField.LabelText = "Status Changed:";
            this.StatusChangedField.LabelTextAlign = System.Drawing.ContentAlignment.TopRight;
            this.StatusChangedField.Location = new System.Drawing.Point(12, 96);
            this.StatusChangedField.Name = "StatusChangedField";
            this.StatusChangedField.Size = new System.Drawing.Size(240, 15);
            this.StatusChangedField.TabIndex = 5;
            this.StatusChangedField.Value = "";
            this.StatusChangedField.ValueTextAlign = System.Drawing.ContentAlignment.TopLeft;
            // 
            // SteamIdWarningIcon
            // 
            this.SteamIdWarningIcon.Image = global::ValheimServerGUI.Properties.Resources.StatusWarning_16x;
            this.SteamIdWarningIcon.Location = new System.Drawing.Point(258, 12);
            this.SteamIdWarningIcon.Name = "SteamIdWarningIcon";
            this.SteamIdWarningIcon.Size = new System.Drawing.Size(16, 16);
            this.SteamIdWarningIcon.TabIndex = 7;
            this.SteamIdWarningIcon.TabStop = false;
            this.SteamIdWarningIcon.Visible = false;
            // 
            // SteamIdCopyButton
            // 
            this.SteamIdCopyButton.CopyFunction = null;
            this.SteamIdCopyButton.Location = new System.Drawing.Point(258, 34);
            this.SteamIdCopyButton.Name = "SteamIdCopyButton";
            this.SteamIdCopyButton.Size = new System.Drawing.Size(16, 16);
            this.SteamIdCopyButton.TabIndex = 2;
            this.SteamIdCopyButton.TabStop = false;
            // 
            // PlayerDetailsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(289, 155);
            this.Controls.Add(this.SteamIdCopyButton);
            this.Controls.Add(this.SteamIdWarningIcon);
            this.Controls.Add(this.StatusChangedField);
            this.Controls.Add(this.OnlineStatusField);
            this.Controls.Add(this.ZdoIdField);
            this.Controls.Add(this.SteamIdField);
            this.Controls.Add(this.ButtonRefresh);
            this.Controls.Add(this.ButtonOK);
            this.Controls.Add(this.PlayerNameField);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PlayerDetailsForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Player Info";
            ((System.ComponentModel.ISupportInitialize)(this.SteamIdWarningIcon)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private ValheimServerGUI.Controls.LabelField PlayerNameField;
        private System.Windows.Forms.Button ButtonOK;
        private System.Windows.Forms.Button ButtonRefresh;
        private ValheimServerGUI.Controls.LabelField SteamIdField;
        private ValheimServerGUI.Controls.LabelField ZdoIdField;
        private ValheimServerGUI.Controls.LabelField OnlineStatusField;
        private ValheimServerGUI.Controls.LabelField StatusChangedField;
        private System.Windows.Forms.PictureBox SteamIdWarningIcon;
        private CopyButton SteamIdCopyButton;
    }
}