namespace ValheimServerGUI.Controls
{
    partial class SelectListField
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
            ListBox = new System.Windows.Forms.ListBox();
            Label = new System.Windows.Forms.Label();
            HelpLabel = new HelpLabel();
            SuspendLayout();
            // 
            // ListBox
            // 
            ListBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            ListBox.FormattingEnabled = true;
            ListBox.ItemHeight = 15;
            ListBox.Items.AddRange(new object[] { "Item 1", "Item 2", "Item 3", "Item 4", "Item 5" });
            ListBox.Location = new System.Drawing.Point(3, 21);
            ListBox.Name = "ListBox";
            ListBox.ScrollAlwaysVisible = true;
            ListBox.Size = new System.Drawing.Size(144, 79);
            ListBox.TabIndex = 0;
            // 
            // Label
            // 
            Label.AutoSize = true;
            Label.Location = new System.Drawing.Point(3, 3);
            Label.Name = "Label";
            Label.Size = new System.Drawing.Size(35, 15);
            Label.TabIndex = 0;
            Label.Text = "Label";
            // 
            // HelpLabel
            // 
            HelpLabel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            HelpLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point);
            HelpLabel.ForeColor = System.Drawing.SystemColors.HotTrack;
            HelpLabel.Location = new System.Drawing.Point(135, 3);
            HelpLabel.Name = "HelpLabel";
            HelpLabel.Size = new System.Drawing.Size(12, 15);
            HelpLabel.TabIndex = 0;
            HelpLabel.TabStop = false;
            // 
            // SelectListField
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(HelpLabel);
            Controls.Add(Label);
            Controls.Add(ListBox);
            Name = "SelectListField";
            Size = new System.Drawing.Size(150, 104);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ListBox ListBox;
        private System.Windows.Forms.Label Label;
        private HelpLabel HelpLabel;
    }
}
