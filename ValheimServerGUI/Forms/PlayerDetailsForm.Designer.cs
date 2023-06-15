
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
            PlayerNameField = new ValheimServerGUI.Controls.LabelField();
            ButtonOK = new System.Windows.Forms.Button();
            ButtonRefresh = new System.Windows.Forms.Button();
            PlatformIdField = new ValheimServerGUI.Controls.LabelField();
            ZdoIdField = new ValheimServerGUI.Controls.LabelField();
            OnlineStatusField = new ValheimServerGUI.Controls.LabelField();
            StatusChangedField = new ValheimServerGUI.Controls.LabelField();
            SteamIdCopyButton = new CopyButton();
            CharacterListField = new AddRemoveListField();
            PlatformIcon = new System.Windows.Forms.PictureBox();
            CurrentCharacterNameField = new ValheimServerGUI.Controls.LabelField();
            PlayerNameEditButton = new EditButton();
            ((System.ComponentModel.ISupportInitialize)PlatformIcon).BeginInit();
            SuspendLayout();
            // 
            // PlayerNameField
            // 
            PlayerNameField.HelpText = "The player's Steam/Xbox username. This will be determined\r\nautomatically, but in case it doesn't work, you may edit the\r\nplayer's name manually.";
            PlayerNameField.LabelSplitRatio = 0.5D;
            PlayerNameField.LabelText = "Player Name:";
            PlayerNameField.LabelTextAlign = System.Drawing.ContentAlignment.TopRight;
            PlayerNameField.Location = new System.Drawing.Point(12, 12);
            PlayerNameField.Name = "PlayerNameField";
            PlayerNameField.Size = new System.Drawing.Size(240, 15);
            PlayerNameField.TabIndex = 0;
            PlayerNameField.Value = "";
            PlayerNameField.ValueTextAlign = System.Drawing.ContentAlignment.TopLeft;
            // 
            // ButtonOK
            // 
            ButtonOK.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            ButtonOK.Location = new System.Drawing.Point(199, 277);
            ButtonOK.Name = "ButtonOK";
            ButtonOK.Size = new System.Drawing.Size(75, 23);
            ButtonOK.TabIndex = 7;
            ButtonOK.Text = "OK";
            ButtonOK.UseVisualStyleBackColor = true;
            // 
            // ButtonRefresh
            // 
            ButtonRefresh.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            ButtonRefresh.Image = Properties.Resources.Restart_16x;
            ButtonRefresh.Location = new System.Drawing.Point(170, 277);
            ButtonRefresh.Name = "ButtonRefresh";
            ButtonRefresh.Size = new System.Drawing.Size(23, 23);
            ButtonRefresh.TabIndex = 6;
            ButtonRefresh.UseVisualStyleBackColor = true;
            // 
            // PlatformIdField
            // 
            PlatformIdField.HelpText = "The ID associated with the player's Steam or Xbox account.\r\nYou can use this ID to allow or ban players, or add players as admins.";
            PlatformIdField.LabelSplitRatio = 0.5D;
            PlatformIdField.LabelText = "Platform ID:";
            PlatformIdField.LabelTextAlign = System.Drawing.ContentAlignment.TopRight;
            PlatformIdField.Location = new System.Drawing.Point(12, 33);
            PlatformIdField.Name = "PlatformIdField";
            PlatformIdField.Size = new System.Drawing.Size(240, 15);
            PlatformIdField.TabIndex = 1;
            PlatformIdField.Value = "";
            PlatformIdField.ValueTextAlign = System.Drawing.ContentAlignment.TopLeft;
            // 
            // ZdoIdField
            // 
            ZdoIdField.HelpText = "The player's object ID in game. This changes with each game session.";
            ZdoIdField.LabelSplitRatio = 0.5D;
            ZdoIdField.LabelText = "ZDOID:";
            ZdoIdField.LabelTextAlign = System.Drawing.ContentAlignment.TopRight;
            ZdoIdField.Location = new System.Drawing.Point(12, 54);
            ZdoIdField.Name = "ZdoIdField";
            ZdoIdField.Size = new System.Drawing.Size(240, 15);
            ZdoIdField.TabIndex = 3;
            ZdoIdField.Value = "";
            ZdoIdField.ValueTextAlign = System.Drawing.ContentAlignment.TopLeft;
            // 
            // OnlineStatusField
            // 
            OnlineStatusField.HelpText = "Possible statuses are: Online, Offline, Joining, or Leaving";
            OnlineStatusField.LabelSplitRatio = 0.5D;
            OnlineStatusField.LabelText = "Online Status:";
            OnlineStatusField.LabelTextAlign = System.Drawing.ContentAlignment.TopRight;
            OnlineStatusField.Location = new System.Drawing.Point(12, 96);
            OnlineStatusField.Name = "OnlineStatusField";
            OnlineStatusField.Size = new System.Drawing.Size(240, 15);
            OnlineStatusField.TabIndex = 4;
            OnlineStatusField.Value = "";
            OnlineStatusField.ValueTextAlign = System.Drawing.ContentAlignment.TopLeft;
            // 
            // StatusChangedField
            // 
            StatusChangedField.HelpText = "";
            StatusChangedField.LabelSplitRatio = 0.5D;
            StatusChangedField.LabelText = "Status Changed:";
            StatusChangedField.LabelTextAlign = System.Drawing.ContentAlignment.TopRight;
            StatusChangedField.Location = new System.Drawing.Point(12, 117);
            StatusChangedField.Name = "StatusChangedField";
            StatusChangedField.Size = new System.Drawing.Size(240, 15);
            StatusChangedField.TabIndex = 5;
            StatusChangedField.Value = "";
            StatusChangedField.ValueTextAlign = System.Drawing.ContentAlignment.TopLeft;
            // 
            // SteamIdCopyButton
            // 
            SteamIdCopyButton.CopyFunction = null;
            SteamIdCopyButton.HelpText = "Copy Steam ID to clipboard";
            SteamIdCopyButton.Location = new System.Drawing.Point(258, 34);
            SteamIdCopyButton.Name = "SteamIdCopyButton";
            SteamIdCopyButton.Size = new System.Drawing.Size(16, 16);
            SteamIdCopyButton.TabIndex = 2;
            SteamIdCopyButton.TabStop = false;
            // 
            // CharacterListField
            // 
            CharacterListField.AddEnabled = true;
            CharacterListField.AddFunction = null;
            CharacterListField.AllowDuplicates = false;
            CharacterListField.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            CharacterListField.EditEnabled = true;
            CharacterListField.EditFunction = null;
            CharacterListField.HelpText = resources.GetString("CharacterListField.HelpText");
            CharacterListField.LabelText = "Known Characters";
            CharacterListField.Location = new System.Drawing.Point(12, 138);
            CharacterListField.Name = "CharacterListField";
            CharacterListField.RemoveEnabled = true;
            CharacterListField.RemoveFunction = null;
            CharacterListField.Size = new System.Drawing.Size(265, 133);
            CharacterListField.TabIndex = 8;
            CharacterListField.Value = null;
            // 
            // PlatformIcon
            // 
            PlatformIcon.ErrorImage = null;
            PlatformIcon.InitialImage = null;
            PlatformIcon.Location = new System.Drawing.Point(38, 11);
            PlatformIcon.Name = "PlatformIcon";
            PlatformIcon.Size = new System.Drawing.Size(16, 16);
            PlatformIcon.TabIndex = 9;
            PlatformIcon.TabStop = false;
            // 
            // CurrentCharacterNameField
            // 
            CurrentCharacterNameField.HelpText = "The character that this player is currently playing, or\r\nif they're offline, the last character they logged in as.";
            CurrentCharacterNameField.LabelSplitRatio = 0.5D;
            CurrentCharacterNameField.LabelText = "Latest Character:";
            CurrentCharacterNameField.LabelTextAlign = System.Drawing.ContentAlignment.TopRight;
            CurrentCharacterNameField.Location = new System.Drawing.Point(12, 75);
            CurrentCharacterNameField.Name = "CurrentCharacterNameField";
            CurrentCharacterNameField.Size = new System.Drawing.Size(240, 15);
            CurrentCharacterNameField.TabIndex = 10;
            CurrentCharacterNameField.Value = "";
            CurrentCharacterNameField.ValueTextAlign = System.Drawing.ContentAlignment.TopLeft;
            // 
            // PlayerNameEditButton
            // 
            PlayerNameEditButton.EditFunction = null;
            PlayerNameEditButton.HelpText = "Edit Player Name";
            PlayerNameEditButton.Location = new System.Drawing.Point(258, 12);
            PlayerNameEditButton.Name = "PlayerNameEditButton";
            PlayerNameEditButton.Size = new System.Drawing.Size(16, 16);
            PlayerNameEditButton.TabIndex = 11;
            // 
            // PlayerDetailsForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(289, 320);
            Controls.Add(PlayerNameEditButton);
            Controls.Add(CurrentCharacterNameField);
            Controls.Add(PlatformIcon);
            Controls.Add(CharacterListField);
            Controls.Add(SteamIdCopyButton);
            Controls.Add(StatusChangedField);
            Controls.Add(OnlineStatusField);
            Controls.Add(ZdoIdField);
            Controls.Add(PlatformIdField);
            Controls.Add(ButtonRefresh);
            Controls.Add(ButtonOK);
            Controls.Add(PlayerNameField);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "PlayerDetailsForm";
            ShowInTaskbar = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Player Info";
            ((System.ComponentModel.ISupportInitialize)PlatformIcon).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private ValheimServerGUI.Controls.LabelField PlayerNameField;
        private System.Windows.Forms.Button ButtonOK;
        private System.Windows.Forms.Button ButtonRefresh;
        private ValheimServerGUI.Controls.LabelField PlatformIdField;
        private ValheimServerGUI.Controls.LabelField ZdoIdField;
        private ValheimServerGUI.Controls.LabelField OnlineStatusField;
        private ValheimServerGUI.Controls.LabelField StatusChangedField;
        private CopyButton SteamIdCopyButton;
        private AddRemoveListField CharacterListField;
        private System.Windows.Forms.PictureBox PlatformIcon;
        private ValheimServerGUI.Controls.LabelField CurrentCharacterNameField;
        private EditButton PlayerNameEditButton;
    }
}