
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
            this.ButtonCancel = new System.Windows.Forms.Button();
            this.ButtonOK = new System.Windows.Forms.Button();
            this.ButtonDefaults = new System.Windows.Forms.Button();
            this.CheckForUpdatesField = new ValheimServerGUI.Controls.CheckboxFormField();
            this.SaveProfileOnStartField = new ValheimServerGUI.Controls.CheckboxFormField();
            this.StartWithWindowsField = new ValheimServerGUI.Controls.CheckboxFormField();
            this.StartMinimizedField = new ValheimServerGUI.Controls.CheckboxFormField();
            this.SuspendLayout();
            // 
            // ButtonCancel
            // 
            this.ButtonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonCancel.Location = new System.Drawing.Point(272, 182);
            this.ButtonCancel.Name = "ButtonCancel";
            this.ButtonCancel.Size = new System.Drawing.Size(75, 23);
            this.ButtonCancel.TabIndex = 6;
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
            this.ButtonOK.TabIndex = 5;
            this.ButtonOK.Text = "OK";
            this.ButtonOK.UseVisualStyleBackColor = true;
            this.ButtonOK.Click += new System.EventHandler(this.ButtonOK_Click);
            // 
            // ButtonDefaults
            // 
            this.ButtonDefaults.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ButtonDefaults.Location = new System.Drawing.Point(12, 182);
            this.ButtonDefaults.Name = "ButtonDefaults";
            this.ButtonDefaults.Size = new System.Drawing.Size(111, 23);
            this.ButtonDefaults.TabIndex = 4;
            this.ButtonDefaults.Text = "Restore Defaults";
            this.ButtonDefaults.UseVisualStyleBackColor = true;
            this.ButtonDefaults.Click += new System.EventHandler(this.ButtonDefaults_Click);
            // 
            // CheckForUpdatesField
            // 
            this.CheckForUpdatesField.HelpText = resources.GetString("CheckForUpdatesField.HelpText");
            this.CheckForUpdatesField.LabelText = "Automatically check for updates";
            this.CheckForUpdatesField.Location = new System.Drawing.Point(12, 35);
            this.CheckForUpdatesField.Name = "CheckForUpdatesField";
            this.CheckForUpdatesField.Size = new System.Drawing.Size(334, 17);
            this.CheckForUpdatesField.TabIndex = 1;
            this.CheckForUpdatesField.Value = false;
            // 
            // SaveProfileOnStartField
            // 
            this.SaveProfileOnStartField.HelpText = "If enabled, any changes you made to your server profile\r\nwill be saved when you c" +
    "lick Start Server. Otherwise, you\r\nmust manually save changes with File > Save.";
            this.SaveProfileOnStartField.LabelText = "Auto save profile when starting server";
            this.SaveProfileOnStartField.Location = new System.Drawing.Point(12, 12);
            this.SaveProfileOnStartField.Name = "SaveProfileOnStartField";
            this.SaveProfileOnStartField.Size = new System.Drawing.Size(334, 17);
            this.SaveProfileOnStartField.TabIndex = 0;
            this.SaveProfileOnStartField.Value = false;
            // 
            // StartWithWindowsField
            // 
            this.StartWithWindowsField.HelpText = "To start your server(s) on Windows startup, enable this setting \r\nalong with \"Sta" +
    "rt this server when ValheimServerGUI starts\"\r\nunder Advanced Controls for each s" +
    "erver.";
            this.StartWithWindowsField.LabelText = "Start ValheimServerGUI with Windows";
            this.StartWithWindowsField.Location = new System.Drawing.Point(12, 58);
            this.StartWithWindowsField.Name = "StartWithWindowsField";
            this.StartWithWindowsField.Size = new System.Drawing.Size(334, 17);
            this.StartWithWindowsField.TabIndex = 2;
            this.StartWithWindowsField.Value = false;
            // 
            // StartMinimizedField
            // 
            this.StartMinimizedField.HelpText = "";
            this.StartMinimizedField.LabelText = "Start ValheimServerGUI minimized";
            this.StartMinimizedField.Location = new System.Drawing.Point(12, 81);
            this.StartMinimizedField.Name = "StartMinimizedField";
            this.StartMinimizedField.Size = new System.Drawing.Size(334, 17);
            this.StartMinimizedField.TabIndex = 3;
            this.StartMinimizedField.Value = false;
            // 
            // PreferencesForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(359, 217);
            this.Controls.Add(this.StartMinimizedField);
            this.Controls.Add(this.StartWithWindowsField);
            this.Controls.Add(this.SaveProfileOnStartField);
            this.Controls.Add(this.CheckForUpdatesField);
            this.Controls.Add(this.ButtonDefaults);
            this.Controls.Add(this.ButtonOK);
            this.Controls.Add(this.ButtonCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PreferencesForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Preferences";
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button ButtonCancel;
        private System.Windows.Forms.Button ButtonOK;
        private System.Windows.Forms.Button ButtonDefaults;
        private ValheimServerGUI.Controls.CheckboxFormField CheckForUpdatesField;
        private ValheimServerGUI.Controls.CheckboxFormField SaveProfileOnStartField;
        private ValheimServerGUI.Controls.CheckboxFormField StartWithWindowsField;
        private ValheimServerGUI.Controls.CheckboxFormField StartMinimizedField;
    }
}