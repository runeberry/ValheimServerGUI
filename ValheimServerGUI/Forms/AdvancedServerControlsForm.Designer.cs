namespace ValheimServerGUI.Forms
{
    partial class AdvancedServerControlsForm
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
            this.ButtonDefaults = new System.Windows.Forms.Button();
            this.ButtonCancel = new System.Windows.Forms.Button();
            this.ButtonOK = new System.Windows.Forms.Button();
            this.SavingGroupBox = new System.Windows.Forms.GroupBox();
            this.ShortBackupIntervalField = new ValheimServerGUI.Controls.NumericFormField();
            this.LongBackupIntervalField = new ValheimServerGUI.Controls.NumericFormField();
            this.SaveIntervalField = new ValheimServerGUI.Controls.NumericFormField();
            this.BackupsField = new ValheimServerGUI.Controls.NumericFormField();
            this.LogFileDirectoryField = new ValheimServerGUI.Controls.FilenameFormField();
            this.SavingGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // ButtonDefaults
            // 
            this.ButtonDefaults.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ButtonDefaults.Location = new System.Drawing.Point(12, 182);
            this.ButtonDefaults.Name = "ButtonDefaults";
            this.ButtonDefaults.Size = new System.Drawing.Size(111, 23);
            this.ButtonDefaults.TabIndex = 6;
            this.ButtonDefaults.Text = "Restore Defaults";
            this.ButtonDefaults.UseVisualStyleBackColor = true;
            this.ButtonDefaults.Click += new System.EventHandler(this.ButtonDefaults_Click);
            // 
            // ButtonCancel
            // 
            this.ButtonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonCancel.Location = new System.Drawing.Point(272, 182);
            this.ButtonCancel.Name = "ButtonCancel";
            this.ButtonCancel.Size = new System.Drawing.Size(75, 23);
            this.ButtonCancel.TabIndex = 7;
            this.ButtonCancel.Text = "Cancel";
            this.ButtonCancel.UseVisualStyleBackColor = true;
            this.ButtonCancel.Click += new System.EventHandler(this.ButtonCancel_Click);
            // 
            // ButtonOK
            // 
            this.ButtonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonOK.Location = new System.Drawing.Point(191, 182);
            this.ButtonOK.Name = "ButtonOK";
            this.ButtonOK.Size = new System.Drawing.Size(75, 23);
            this.ButtonOK.TabIndex = 8;
            this.ButtonOK.Text = "OK";
            this.ButtonOK.UseVisualStyleBackColor = true;
            this.ButtonOK.Click += new System.EventHandler(this.ButtonOK_Click);
            // 
            // SavingGroupBox
            // 
            this.SavingGroupBox.Controls.Add(this.ShortBackupIntervalField);
            this.SavingGroupBox.Controls.Add(this.LongBackupIntervalField);
            this.SavingGroupBox.Controls.Add(this.SaveIntervalField);
            this.SavingGroupBox.Controls.Add(this.BackupsField);
            this.SavingGroupBox.Location = new System.Drawing.Point(12, 12);
            this.SavingGroupBox.Name = "SavingGroupBox";
            this.SavingGroupBox.Size = new System.Drawing.Size(335, 123);
            this.SavingGroupBox.TabIndex = 9;
            this.SavingGroupBox.TabStop = false;
            this.SavingGroupBox.Text = "Saving";
            // 
            // ShortBackupIntervalField
            // 
            this.ShortBackupIntervalField.HelpText = "How often to create a rolling backup of the world\r\nsave data, in seconds.";
            this.ShortBackupIntervalField.LabelText = "Short Backup Interval";
            this.ShortBackupIntervalField.Location = new System.Drawing.Point(117, 22);
            this.ShortBackupIntervalField.Maximum = 2592000;
            this.ShortBackupIntervalField.Minimum = 300;
            this.ShortBackupIntervalField.Name = "ShortBackupIntervalField";
            this.ShortBackupIntervalField.Size = new System.Drawing.Size(153, 41);
            this.ShortBackupIntervalField.TabIndex = 3;
            this.ShortBackupIntervalField.Value = 300;
            // 
            // LongBackupIntervalField
            // 
            this.LongBackupIntervalField.HelpText = "How often to create additional backups of the world\r\nsave data, in seconds. This " +
    "interval must be longer than\r\nthe short backup interval.";
            this.LongBackupIntervalField.LabelText = "Long Backup Interval";
            this.LongBackupIntervalField.Location = new System.Drawing.Point(117, 69);
            this.LongBackupIntervalField.Maximum = 2592000;
            this.LongBackupIntervalField.Minimum = 300;
            this.LongBackupIntervalField.Name = "LongBackupIntervalField";
            this.LongBackupIntervalField.Size = new System.Drawing.Size(153, 41);
            this.LongBackupIntervalField.TabIndex = 2;
            this.LongBackupIntervalField.Value = 300;
            // 
            // SaveIntervalField
            // 
            this.SaveIntervalField.HelpText = "How often the world is saved, in seconds.";
            this.SaveIntervalField.LabelText = "Save Interval";
            this.SaveIntervalField.Location = new System.Drawing.Point(6, 22);
            this.SaveIntervalField.Maximum = 86400;
            this.SaveIntervalField.Minimum = 60;
            this.SaveIntervalField.Name = "SaveIntervalField";
            this.SaveIntervalField.Size = new System.Drawing.Size(105, 41);
            this.SaveIntervalField.TabIndex = 1;
            this.SaveIntervalField.Value = 60;
            // 
            // BackupsField
            // 
            this.BackupsField.HelpText = "Number of world data backups to maintain. One rolling backup\r\nis created on the s" +
    "hort backup interval, and subsequent backups are\r\ncreated on the long backup int" +
    "erval.";
            this.BackupsField.LabelText = "Backups";
            this.BackupsField.Location = new System.Drawing.Point(6, 69);
            this.BackupsField.Maximum = 1000;
            this.BackupsField.Minimum = 1;
            this.BackupsField.Name = "BackupsField";
            this.BackupsField.Size = new System.Drawing.Size(105, 41);
            this.BackupsField.TabIndex = 0;
            this.BackupsField.Value = 1;
            // 
            // LogFileDirectoryField
            // 
            this.LogFileDirectoryField.FileSelectMode = ValheimServerGUI.Controls.FileSelectMode.Directory;
            this.LogFileDirectoryField.HelpText = "If you would like server logs saved to a file, enter a\r\ndirectory here. A new tim" +
    "estamped log file will be\r\ncreated when the server is started.\r\n";
            this.LogFileDirectoryField.InitialPath = null;
            this.LogFileDirectoryField.LabelText = "Log File Directory";
            this.LogFileDirectoryField.Location = new System.Drawing.Point(12, 135);
            this.LogFileDirectoryField.MultiFileSeparator = "; ";
            this.LogFileDirectoryField.Name = "LogFileDirectoryField";
            this.LogFileDirectoryField.ReadOnly = false;
            this.LogFileDirectoryField.Size = new System.Drawing.Size(335, 41);
            this.LogFileDirectoryField.TabIndex = 10;
            this.LogFileDirectoryField.Value = "";
            this.LogFileDirectoryField.Visible = false;
            // 
            // AdvancedServerControlsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(359, 217);
            this.Controls.Add(this.LogFileDirectoryField);
            this.Controls.Add(this.SavingGroupBox);
            this.Controls.Add(this.ButtonOK);
            this.Controls.Add(this.ButtonCancel);
            this.Controls.Add(this.ButtonDefaults);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AdvancedServerControlsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Advanced Server Controls";
            this.SavingGroupBox.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button ButtonDefaults;
        private System.Windows.Forms.Button ButtonCancel;
        private System.Windows.Forms.Button ButtonOK;
        private System.Windows.Forms.GroupBox SavingGroupBox;
        private ValheimServerGUI.Controls.NumericFormField ShortBackupIntervalField;
        private ValheimServerGUI.Controls.NumericFormField SaveIntervalField;
        private ValheimServerGUI.Controls.NumericFormField BackupsField;
        private ValheimServerGUI.Controls.NumericFormField LongBackupIntervalField;
        private ValheimServerGUI.Controls.FilenameFormField LogFileDirectoryField;
    }
}