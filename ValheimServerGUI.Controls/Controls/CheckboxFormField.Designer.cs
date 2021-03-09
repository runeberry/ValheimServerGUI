
namespace ValheimServerGUI.Controls
{
    partial class CheckboxFormField
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
            this.components = new System.ComponentModel.Container();
            this.CheckBox = new System.Windows.Forms.CheckBox();
            this.HelpToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.HelpLabel = new ValheimServerGUI.Controls.HelpLabel();
            this.SuspendLayout();
            // 
            // CheckBox
            // 
            this.CheckBox.AutoSize = true;
            this.CheckBox.Location = new System.Drawing.Point(9, 0);
            this.CheckBox.Name = "CheckBox";
            this.CheckBox.Size = new System.Drawing.Size(54, 19);
            this.CheckBox.TabIndex = 0;
            this.CheckBox.Text = "Label";
            this.CheckBox.UseVisualStyleBackColor = true;
            // 
            // HelpToolTip
            // 
            this.HelpToolTip.AutoPopDelay = 30000;
            this.HelpToolTip.InitialDelay = 500;
            this.HelpToolTip.ReshowDelay = 100;
            // 
            // HelpLabel
            // 
            this.HelpLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.HelpLabel.Font = new System.Drawing.Font("Segoe UI", 9F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline))), System.Drawing.GraphicsUnit.Point);
            this.HelpLabel.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.HelpLabel.Location = new System.Drawing.Point(138, 0);
            this.HelpLabel.Name = "HelpLabel";
            this.HelpLabel.Size = new System.Drawing.Size(12, 15);
            this.HelpLabel.TabIndex = 8;
            // 
            // CheckboxFormField
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.HelpLabel);
            this.Controls.Add(this.CheckBox);
            this.Name = "CheckboxFormField";
            this.Size = new System.Drawing.Size(150, 17);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox CheckBox;
        private System.Windows.Forms.ToolTip HelpToolTip;
        private HelpLabel HelpLabel;
    }
}
