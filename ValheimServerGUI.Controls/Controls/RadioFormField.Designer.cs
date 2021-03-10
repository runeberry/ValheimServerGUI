
namespace ValheimServerGUI.Controls
{
    partial class RadioFormField
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
            this.RadioButton = new System.Windows.Forms.RadioButton();
            this.HelpLabel = new ValheimServerGUI.Controls.HelpLabel();
            this.SuspendLayout();
            // 
            // RadioButton
            // 
            this.RadioButton.AutoSize = true;
            this.RadioButton.Location = new System.Drawing.Point(9, -1);
            this.RadioButton.Name = "RadioButton";
            this.RadioButton.Size = new System.Drawing.Size(53, 19);
            this.RadioButton.TabIndex = 0;
            this.RadioButton.TabStop = true;
            this.RadioButton.Text = "Label";
            this.RadioButton.UseVisualStyleBackColor = true;
            // 
            // HelpLabel
            // 
            this.HelpLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.HelpLabel.Font = new System.Drawing.Font("Segoe UI", 9F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline))), System.Drawing.GraphicsUnit.Point);
            this.HelpLabel.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.HelpLabel.Location = new System.Drawing.Point(138, 0);
            this.HelpLabel.Name = "HelpLabel";
            this.HelpLabel.Size = new System.Drawing.Size(12, 15);
            this.HelpLabel.TabIndex = 9;
            // 
            // RadioFormField
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.HelpLabel);
            this.Controls.Add(this.RadioButton);
            this.Name = "RadioFormField";
            this.Size = new System.Drawing.Size(150, 17);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RadioButton RadioButton;
        private HelpLabel HelpLabel;
    }
}
