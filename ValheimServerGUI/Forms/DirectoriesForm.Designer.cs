
namespace ValheimServerGUI.Forms
{
    partial class DirectoriesForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DirectoriesForm));
            this.ButtonCancel = new System.Windows.Forms.Button();
            this.ButtonOK = new System.Windows.Forms.Button();
            this.ButtonDefaults = new System.Windows.Forms.Button();
            this.ServerExePathField = new ValheimServerGUI.Controls.FilenameFormField();
            this.SaveDataFolderPathField = new ValheimServerGUI.Controls.FilenameFormField();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // ButtonCancel
            // 
            this.ButtonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonCancel.Location = new System.Drawing.Point(272, 182);
            this.ButtonCancel.Name = "ButtonCancel";
            this.ButtonCancel.Size = new System.Drawing.Size(75, 23);
            this.ButtonCancel.TabIndex = 4;
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
            this.ButtonOK.TabIndex = 3;
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
            this.ButtonDefaults.TabIndex = 2;
            this.ButtonDefaults.Text = "Restore Defaults";
            this.ButtonDefaults.UseVisualStyleBackColor = true;
            this.ButtonDefaults.Click += new System.EventHandler(this.ButtonDefaults_Click);
            // 
            // ServerExePathField
            // 
            this.ServerExePathField.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ServerExePathField.FileSelectMode = ValheimServerGUI.Controls.FileSelectMode.SingleFile;
            this.ServerExePathField.HelpText = resources.GetString("ServerExePathField.HelpText");
            this.ServerExePathField.InitialPath = null;
            this.ServerExePathField.LabelText = "Valheim Dedicated Server .exe";
            this.ServerExePathField.Location = new System.Drawing.Point(12, 45);
            this.ServerExePathField.MultiFileSeparator = "; ";
            this.ServerExePathField.Name = "ServerExePathField";
            this.ServerExePathField.ReadOnly = false;
            this.ServerExePathField.Size = new System.Drawing.Size(335, 45);
            this.ServerExePathField.TabIndex = 0;
            this.ServerExePathField.Value = "";
            // 
            // SaveDataFolderPathField
            // 
            this.SaveDataFolderPathField.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SaveDataFolderPathField.FileSelectMode = ValheimServerGUI.Controls.FileSelectMode.Directory;
            this.SaveDataFolderPathField.HelpText = "The location of the save data for your Valheim server,\r\nincluding the \"worlds\" fo" +
    "lder, admin lists, etc.";
            this.SaveDataFolderPathField.InitialPath = null;
            this.SaveDataFolderPathField.LabelText = "Valheim Save Data Folder";
            this.SaveDataFolderPathField.Location = new System.Drawing.Point(12, 96);
            this.SaveDataFolderPathField.MultiFileSeparator = "; ";
            this.SaveDataFolderPathField.Name = "SaveDataFolderPathField";
            this.SaveDataFolderPathField.ReadOnly = false;
            this.SaveDataFolderPathField.Size = new System.Drawing.Size(335, 41);
            this.SaveDataFolderPathField.TabIndex = 1;
            this.SaveDataFolderPathField.Value = "";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point);
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(316, 30);
            this.label1.TabIndex = 0;
            this.label1.Text = "These paths are used for all profiles. You can override these\r\npaths for individu" +
    "al profiles in the Advanced Controls tab.";
            // 
            // DirectoriesForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(359, 217);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.SaveDataFolderPathField);
            this.Controls.Add(this.ServerExePathField);
            this.Controls.Add(this.ButtonDefaults);
            this.Controls.Add(this.ButtonOK);
            this.Controls.Add(this.ButtonCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DirectoriesForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Directories";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button ButtonCancel;
        private System.Windows.Forms.Button ButtonOK;
        private System.Windows.Forms.Button ButtonDefaults;
        private ValheimServerGUI.Controls.FilenameFormField ServerExePathField;
        private ValheimServerGUI.Controls.FilenameFormField SaveDataFolderPathField;
        private System.Windows.Forms.Label label1;
    }
}