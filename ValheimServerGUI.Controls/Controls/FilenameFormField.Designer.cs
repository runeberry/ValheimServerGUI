
namespace ValheimServerGUI.Controls
{
    partial class FilenameFormField
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.Label = new System.Windows.Forms.Label();
            this.TextBox = new System.Windows.Forms.TextBox();
            this.FileBrowserButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // Label
            // 
            this.Label.AutoSize = true;
            this.Label.Location = new System.Drawing.Point(9, 0);
            this.Label.Name = "Label";
            this.Label.Size = new System.Drawing.Size(35, 15);
            this.Label.TabIndex = 3;
            this.Label.Text = "Label";
            // 
            // TextBox
            // 
            this.TextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TextBox.Location = new System.Drawing.Point(9, 18);
            this.TextBox.Name = "TextBox";
            this.TextBox.Size = new System.Drawing.Size(106, 23);
            this.TextBox.TabIndex = 2;
            // 
            // FileBrowserButton
            // 
            this.FileBrowserButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.FileBrowserButton.Location = new System.Drawing.Point(116, 18);
            this.FileBrowserButton.Name = "FileBrowserButton";
            this.FileBrowserButton.Size = new System.Drawing.Size(26, 23);
            this.FileBrowserButton.TabIndex = 4;
            this.FileBrowserButton.Text = "...";
            this.FileBrowserButton.UseVisualStyleBackColor = true;
            this.FileBrowserButton.Click += new System.EventHandler(this.FileBrowserButton_Click);
            // 
            // FilenameFormField
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.FileBrowserButton);
            this.Controls.Add(this.Label);
            this.Controls.Add(this.TextBox);
            this.Name = "FilenameFormField";
            this.Size = new System.Drawing.Size(150, 41);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label Label;
        private System.Windows.Forms.TextBox TextBox;
        private System.Windows.Forms.Button FileBrowserButton;
    }
}
