
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
            this.WindowsStartField = new ValheimServerGUI.Controls.CheckboxFormField();
            this.ServerStartField = new ValheimServerGUI.Controls.CheckboxFormField();
            this.StartMinimizedField = new ValheimServerGUI.Controls.CheckboxFormField();
            this.CheckForUpdatesField = new ValheimServerGUI.Controls.CheckboxFormField();
            this.CheckServerRunningField = new ValheimServerGUI.Controls.CheckboxFormField();
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
            // WindowsStartField
            // 
            this.WindowsStartField.HelpText = "";
            this.WindowsStartField.LabelText = "Start ValheimServerGUI when Windows starts";
            this.WindowsStartField.Location = new System.Drawing.Point(12, 12);
            this.WindowsStartField.Name = "WindowsStartField";
            this.WindowsStartField.Size = new System.Drawing.Size(335, 17);
            this.WindowsStartField.TabIndex = 6;
            this.WindowsStartField.Value = false;
            // 
            // ServerStartField
            // 
            this.ServerStartField.HelpText = "";
            this.ServerStartField.LabelText = "Start your server when ValheimServerGUI starts";
            this.ServerStartField.Location = new System.Drawing.Point(12, 35);
            this.ServerStartField.Name = "ServerStartField";
            this.ServerStartField.Size = new System.Drawing.Size(334, 17);
            this.ServerStartField.TabIndex = 7;
            this.ServerStartField.Value = false;
            // 
            // StartMinimizedField
            // 
            this.StartMinimizedField.HelpText = "";
            this.StartMinimizedField.LabelText = "Start ValheimServerGUI minimized";
            this.StartMinimizedField.Location = new System.Drawing.Point(12, 58);
            this.StartMinimizedField.Name = "StartMinimizedField";
            this.StartMinimizedField.Size = new System.Drawing.Size(334, 17);
            this.StartMinimizedField.TabIndex = 8;
            this.StartMinimizedField.Value = false;
            // 
            // CheckForUpdatesField
            // 
            this.CheckForUpdatesField.HelpText = resources.GetString("CheckForUpdatesField.HelpText");
            this.CheckForUpdatesField.LabelText = "Automatically check for updates";
            this.CheckForUpdatesField.Location = new System.Drawing.Point(12, 104);
            this.CheckForUpdatesField.Name = "CheckForUpdatesField";
            this.CheckForUpdatesField.Size = new System.Drawing.Size(334, 17);
            this.CheckForUpdatesField.TabIndex = 9;
            this.CheckForUpdatesField.Value = false;
            // 
            // CheckServerRunningField
            // 
            this.CheckServerRunningField.HelpText = resources.GetString("CheckServerRunningField.HelpText");
            this.CheckServerRunningField.LabelText = "Check if server is already running on startup";
            this.CheckServerRunningField.Location = new System.Drawing.Point(12, 81);
            this.CheckServerRunningField.Name = "CheckServerRunningField";
            this.CheckServerRunningField.Size = new System.Drawing.Size(334, 17);
            this.CheckServerRunningField.TabIndex = 10;
            this.CheckServerRunningField.Value = false;
            // 
            // PreferencesForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(359, 217);
            this.Controls.Add(this.CheckServerRunningField);
            this.Controls.Add(this.CheckForUpdatesField);
            this.Controls.Add(this.StartMinimizedField);
            this.Controls.Add(this.ServerStartField);
            this.Controls.Add(this.WindowsStartField);
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
        private ValheimServerGUI.Controls.CheckboxFormField WindowsStartField;
        private ValheimServerGUI.Controls.CheckboxFormField ServerStartField;
        private ValheimServerGUI.Controls.CheckboxFormField StartMinimizedField;
        private ValheimServerGUI.Controls.CheckboxFormField CheckForUpdatesField;
        private ValheimServerGUI.Controls.CheckboxFormField CheckServerRunningField;
    }
}