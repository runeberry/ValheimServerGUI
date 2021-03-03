
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
            this.GamePathField = new ValheimServerGUI.Forms.Controls.TextFormField();
            this.ServerPathField = new ValheimServerGUI.Forms.Controls.TextFormField();
            this.ButtonCancel = new System.Windows.Forms.Button();
            this.ButtonOK = new System.Windows.Forms.Button();
            this.ButtonDefaults = new System.Windows.Forms.Button();
            this.WorldsFolderField = new ValheimServerGUI.Forms.Controls.TextFormField();
            this.SuspendLayout();
            // 
            // GamePathField
            // 
            this.GamePathField.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.GamePathField.HideValue = false;
            this.GamePathField.LabelText = "Valheim Game Location";
            this.GamePathField.Location = new System.Drawing.Point(12, 12);
            this.GamePathField.Name = "GamePathField";
            this.GamePathField.Size = new System.Drawing.Size(335, 41);
            this.GamePathField.TabIndex = 0;
            this.GamePathField.Value = "";
            // 
            // ServerPathField
            // 
            this.ServerPathField.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ServerPathField.HideValue = false;
            this.ServerPathField.LabelText = "Valheim Server Location";
            this.ServerPathField.Location = new System.Drawing.Point(12, 59);
            this.ServerPathField.Name = "ServerPathField";
            this.ServerPathField.Size = new System.Drawing.Size(335, 41);
            this.ServerPathField.TabIndex = 1;
            this.ServerPathField.Value = "";
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
            // WorldsFolderField
            // 
            this.WorldsFolderField.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.WorldsFolderField.HideValue = false;
            this.WorldsFolderField.LabelText = "Valheim Worlds Folder";
            this.WorldsFolderField.Location = new System.Drawing.Point(12, 106);
            this.WorldsFolderField.Name = "WorldsFolderField";
            this.WorldsFolderField.Size = new System.Drawing.Size(335, 41);
            this.WorldsFolderField.TabIndex = 2;
            this.WorldsFolderField.Value = "";
            // 
            // DirectoriesForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(359, 217);
            this.Controls.Add(this.WorldsFolderField);
            this.Controls.Add(this.ButtonDefaults);
            this.Controls.Add(this.ButtonOK);
            this.Controls.Add(this.ButtonCancel);
            this.Controls.Add(this.ServerPathField);
            this.Controls.Add(this.GamePathField);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DirectoriesForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Directories";
            this.ResumeLayout(false);

        }

        #endregion

        private Controls.TextFormField GamePathField;
        private Controls.TextFormField ServerPathField;
        private System.Windows.Forms.Button ButtonCancel;
        private System.Windows.Forms.Button ButtonOK;
        private System.Windows.Forms.Button ButtonDefaults;
        private Controls.TextFormField WorldsFolderField;
    }
}