
namespace ValheimServerGUI.Forms
{
    partial class PreferencesForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PreferencesForm));
            ButtonCancel = new System.Windows.Forms.Button();
            ButtonOK = new System.Windows.Forms.Button();
            ButtonDefaults = new System.Windows.Forms.Button();
            CheckForUpdatesField = new ValheimServerGUI.Controls.CheckboxFormField();
            SaveProfileOnStartField = new ValheimServerGUI.Controls.CheckboxFormField();
            StartWithWindowsField = new ValheimServerGUI.Controls.CheckboxFormField();
            StartMinimizedField = new ValheimServerGUI.Controls.CheckboxFormField();
            WriteLogFileField = new ValheimServerGUI.Controls.CheckboxFormField();
            PasswordValidationField = new ValheimServerGUI.Controls.CheckboxFormField();
            SuspendLayout();
            // 
            // ButtonCancel
            // 
            ButtonCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            ButtonCancel.Location = new System.Drawing.Point(272, 182);
            ButtonCancel.Name = "ButtonCancel";
            ButtonCancel.Size = new System.Drawing.Size(75, 23);
            ButtonCancel.TabIndex = 8;
            ButtonCancel.Text = "Cancel";
            ButtonCancel.UseVisualStyleBackColor = true;
            ButtonCancel.Click += ButtonCancel_Click;
            // 
            // ButtonOK
            // 
            ButtonOK.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            ButtonOK.Location = new System.Drawing.Point(191, 182);
            ButtonOK.Name = "ButtonOK";
            ButtonOK.Size = new System.Drawing.Size(75, 23);
            ButtonOK.TabIndex = 7;
            ButtonOK.Text = "OK";
            ButtonOK.UseVisualStyleBackColor = true;
            ButtonOK.Click += ButtonOK_Click;
            // 
            // ButtonDefaults
            // 
            ButtonDefaults.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            ButtonDefaults.Location = new System.Drawing.Point(12, 182);
            ButtonDefaults.Name = "ButtonDefaults";
            ButtonDefaults.Size = new System.Drawing.Size(111, 23);
            ButtonDefaults.TabIndex = 6;
            ButtonDefaults.Text = "Restore Defaults";
            ButtonDefaults.UseVisualStyleBackColor = true;
            ButtonDefaults.Click += ButtonDefaults_Click;
            // 
            // CheckForUpdatesField
            // 
            CheckForUpdatesField.HelpText = resources.GetString("CheckForUpdatesField.HelpText");
            CheckForUpdatesField.LabelText = "Automatically check for updates";
            CheckForUpdatesField.Location = new System.Drawing.Point(12, 12);
            CheckForUpdatesField.Name = "CheckForUpdatesField";
            CheckForUpdatesField.Size = new System.Drawing.Size(335, 17);
            CheckForUpdatesField.TabIndex = 0;
            CheckForUpdatesField.Value = false;
            // 
            // SaveProfileOnStartField
            // 
            SaveProfileOnStartField.HelpText = "If enabled, any changes you made to your server profile\r\nwill be saved when you click Start Server. Otherwise, you\r\nmust manually save changes with File > Save.";
            SaveProfileOnStartField.LabelText = "Auto save profile when starting server";
            SaveProfileOnStartField.Location = new System.Drawing.Point(12, 35);
            SaveProfileOnStartField.Name = "SaveProfileOnStartField";
            SaveProfileOnStartField.Size = new System.Drawing.Size(335, 17);
            SaveProfileOnStartField.TabIndex = 1;
            SaveProfileOnStartField.Value = false;
            // 
            // StartWithWindowsField
            // 
            StartWithWindowsField.HelpText = "To start your server(s) on Windows startup, enable this setting \r\nalong with \"Start this server when ValheimServerGUI starts\"\r\nunder Advanced Controls for each server.";
            StartWithWindowsField.LabelText = "Start ValheimServerGUI with Windows";
            StartWithWindowsField.Location = new System.Drawing.Point(12, 104);
            StartWithWindowsField.Name = "StartWithWindowsField";
            StartWithWindowsField.Size = new System.Drawing.Size(335, 17);
            StartWithWindowsField.TabIndex = 4;
            StartWithWindowsField.Value = false;
            // 
            // StartMinimizedField
            // 
            StartMinimizedField.HelpText = "Enable this setting to minimize VSG to the Windows system\r\ntray when it starts up.";
            StartMinimizedField.LabelText = "Start ValheimServerGUI minimized";
            StartMinimizedField.Location = new System.Drawing.Point(12, 127);
            StartMinimizedField.Name = "StartMinimizedField";
            StartMinimizedField.Size = new System.Drawing.Size(335, 17);
            StartMinimizedField.TabIndex = 5;
            StartMinimizedField.Value = false;
            // 
            // WriteLogFileField
            // 
            WriteLogFileField.HelpText = resources.GetString("WriteLogFileField.HelpText");
            WriteLogFileField.LabelText = "Write application logs to file";
            WriteLogFileField.Location = new System.Drawing.Point(12, 81);
            WriteLogFileField.Name = "WriteLogFileField";
            WriteLogFileField.Size = new System.Drawing.Size(335, 17);
            WriteLogFileField.TabIndex = 3;
            WriteLogFileField.Value = false;
            // 
            // PasswordValidationField
            // 
            PasswordValidationField.HelpText = resources.GetString("PasswordValidationField.HelpText");
            PasswordValidationField.LabelText = "Validate password when starting server";
            PasswordValidationField.Location = new System.Drawing.Point(12, 58);
            PasswordValidationField.Name = "PasswordValidationField";
            PasswordValidationField.Size = new System.Drawing.Size(335, 17);
            PasswordValidationField.TabIndex = 2;
            PasswordValidationField.Value = false;
            // 
            // PreferencesForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(359, 217);
            Controls.Add(PasswordValidationField);
            Controls.Add(WriteLogFileField);
            Controls.Add(StartMinimizedField);
            Controls.Add(StartWithWindowsField);
            Controls.Add(SaveProfileOnStartField);
            Controls.Add(CheckForUpdatesField);
            Controls.Add(ButtonDefaults);
            Controls.Add(ButtonOK);
            Controls.Add(ButtonCancel);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "PreferencesForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Preferences";
            ResumeLayout(false);
        }

        #endregion
        private System.Windows.Forms.Button ButtonCancel;
        private System.Windows.Forms.Button ButtonOK;
        private System.Windows.Forms.Button ButtonDefaults;
        private ValheimServerGUI.Controls.CheckboxFormField CheckForUpdatesField;
        private ValheimServerGUI.Controls.CheckboxFormField SaveProfileOnStartField;
        private ValheimServerGUI.Controls.CheckboxFormField StartWithWindowsField;
        private ValheimServerGUI.Controls.CheckboxFormField StartMinimizedField;
        private ValheimServerGUI.Controls.CheckboxFormField WriteLogFileField;
        private ValheimServerGUI.Controls.CheckboxFormField PasswordValidationField;
    }
}