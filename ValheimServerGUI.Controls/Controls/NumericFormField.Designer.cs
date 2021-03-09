
namespace ValheimServerGUI.Controls
{
    partial class NumericFormField
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
            this.NumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.HelpLabel = new ValheimServerGUI.Controls.HelpLabel();
            ((System.ComponentModel.ISupportInitialize)(this.NumericUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // Label
            // 
            this.Label.AutoSize = true;
            this.Label.Location = new System.Drawing.Point(9, 0);
            this.Label.Name = "Label";
            this.Label.Size = new System.Drawing.Size(35, 15);
            this.Label.TabIndex = 2;
            this.Label.Text = "Label";
            // 
            // NumericUpDown
            // 
            this.NumericUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.NumericUpDown.Location = new System.Drawing.Point(9, 18);
            this.NumericUpDown.Name = "NumericUpDown";
            this.NumericUpDown.Size = new System.Drawing.Size(57, 23);
            this.NumericUpDown.TabIndex = 3;
            // 
            // HelpLabel
            // 
            this.HelpLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.HelpLabel.Font = new System.Drawing.Font("Segoe UI", 9F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline))), System.Drawing.GraphicsUnit.Point);
            this.HelpLabel.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.HelpLabel.Location = new System.Drawing.Point(54, 0);
            this.HelpLabel.Name = "HelpLabel";
            this.HelpLabel.Size = new System.Drawing.Size(12, 15);
            this.HelpLabel.TabIndex = 7;
            // 
            // NumericFormField
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.HelpLabel);
            this.Controls.Add(this.NumericUpDown);
            this.Controls.Add(this.Label);
            this.Name = "NumericFormField";
            this.Size = new System.Drawing.Size(75, 41);
            ((System.ComponentModel.ISupportInitialize)(this.NumericUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label Label;
        private System.Windows.Forms.NumericUpDown NumericUpDown;
        private HelpLabel HelpLabel;
    }
}
