
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
            this.SuspendLayout();
            // 
            // ButtonCancel
            // 
            this.ButtonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonCancel.Location = new System.Drawing.Point(272, 182);
            this.ButtonCancel.Name = "ButtonCancel";
            this.ButtonCancel.Size = new System.Drawing.Size(75, 23);
            this.ButtonCancel.TabIndex = 3;
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
            this.ButtonOK.TabIndex = 4;
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
            this.ButtonDefaults.TabIndex = 5;
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
            this.CheckForUpdatesField.TabIndex = 9;
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
            this.SaveProfileOnStartField.TabIndex = 10;
            this.SaveProfileOnStartField.Value = false;
            // 
            // PreferencesForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(359, 217);
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
    }
}