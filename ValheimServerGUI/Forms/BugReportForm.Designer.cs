
namespace ValheimServerGUI.Forms
{
    partial class BugReportForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BugReportForm));
            this.ButtonCancel = new System.Windows.Forms.Button();
            this.ButtonSubmit = new System.Windows.Forms.Button();
            this.ContactInfoField = new ValheimServerGUI.Forms.Controls.TextFormField();
            this.BugReportField = new ValheimServerGUI.Forms.Controls.TextFormField();
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
            // 
            // ButtonSubmit
            // 
            this.ButtonSubmit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonSubmit.Enabled = false;
            this.ButtonSubmit.Location = new System.Drawing.Point(191, 182);
            this.ButtonSubmit.Name = "ButtonSubmit";
            this.ButtonSubmit.Size = new System.Drawing.Size(75, 23);
            this.ButtonSubmit.TabIndex = 2;
            this.ButtonSubmit.Text = "Submit";
            this.ButtonSubmit.UseVisualStyleBackColor = true;
            // 
            // ContactInfoField
            // 
            this.ContactInfoField.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ContactInfoField.HelpText = resources.GetString("ContactInfoField.HelpText");
            this.ContactInfoField.HideValue = false;
            this.ContactInfoField.LabelText = "(Optional) Contact details for follow-up";
            this.ContactInfoField.Location = new System.Drawing.Point(12, 135);
            this.ContactInfoField.MaxLength = 255;
            this.ContactInfoField.Multiline = false;
            this.ContactInfoField.Name = "ContactInfoField";
            this.ContactInfoField.Size = new System.Drawing.Size(334, 41);
            this.ContactInfoField.TabIndex = 1;
            this.ContactInfoField.Value = "";
            // 
            // BugReportField
            // 
            this.BugReportField.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.BugReportField.HelpText = resources.GetString("BugReportField.HelpText");
            this.BugReportField.HideValue = false;
            this.BugReportField.LabelText = "What\'s the problem? And how did you encounter it?";
            this.BugReportField.Location = new System.Drawing.Point(12, 12);
            this.BugReportField.MaxLength = 2000;
            this.BugReportField.Multiline = true;
            this.BugReportField.Name = "BugReportField";
            this.BugReportField.Size = new System.Drawing.Size(334, 117);
            this.BugReportField.TabIndex = 0;
            this.BugReportField.Value = "";
            // 
            // BugReportForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(359, 217);
            this.Controls.Add(this.BugReportField);
            this.Controls.Add(this.ContactInfoField);
            this.Controls.Add(this.ButtonSubmit);
            this.Controls.Add(this.ButtonCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "BugReportForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Submit a Bug Report";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button ButtonCancel;
        private System.Windows.Forms.Button ButtonSubmit;
        private Controls.TextFormField ContactInfoField;
        private Controls.TextFormField BugReportField;
    }
}