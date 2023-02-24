
namespace ValheimServerGUI.Forms.Controls
{
    partial class TextFormField
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
            this.TextBox = new System.Windows.Forms.TextBox();
            this.Label = new System.Windows.Forms.Label();
            this.HelpLabel = new ValheimServerGUI.Controls.HelpLabel();
            this.SuspendLayout();
            // 
            // TextBox
            // 
            this.TextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TextBox.Location = new System.Drawing.Point(9, 18);
            this.TextBox.Name = "TextBox";
            this.TextBox.Size = new System.Drawing.Size(132, 23);
            this.TextBox.TabIndex = 0;
            // 
            // Label
            // 
            this.Label.AutoSize = true;
            this.Label.Location = new System.Drawing.Point(9, 0);
            this.Label.Name = "Label";
            this.Label.Size = new System.Drawing.Size(35, 15);
            this.Label.TabIndex = 0;
            this.Label.Text = "Label";
            // 
            // HelpLabel
            // 
            this.HelpLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.HelpLabel.Font = new System.Drawing.Font("Segoe UI", 9F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline))), System.Drawing.GraphicsUnit.Point);
            this.HelpLabel.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.HelpLabel.Location = new System.Drawing.Point(129, 0);
            this.HelpLabel.Name = "HelpLabel";
            this.HelpLabel.Size = new System.Drawing.Size(12, 15);
            this.HelpLabel.TabIndex = 0;
            this.HelpLabel.TabStop = false;
            // 
            // TextFormField
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.HelpLabel);
            this.Controls.Add(this.Label);
            this.Controls.Add(this.TextBox);
            this.Name = "TextFormField";
            this.Size = new System.Drawing.Size(150, 41);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox TextBox;
        private System.Windows.Forms.Label Label;
        private ValheimServerGUI.Controls.HelpLabel HelpLabel;
    }
}
