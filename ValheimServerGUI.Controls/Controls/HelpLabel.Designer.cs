
namespace ValheimServerGUI.Controls
{
    partial class HelpLabel
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
            this.Label = new System.Windows.Forms.Label();
            this.ToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            // 
            // Label
            // 
            this.Label.AutoSize = true;
            this.Label.Location = new System.Drawing.Point(0, 0);
            this.Label.Name = "Label";
            this.Label.Size = new System.Drawing.Size(12, 15);
            this.Label.TabIndex = 0;
            this.Label.Text = "?";
            this.Label.Visible = false;
            // 
            // ToolTip
            // 
            this.ToolTip.AutoPopDelay = 30000;
            this.ToolTip.InitialDelay = 500;
            this.ToolTip.ReshowDelay = 100;
            // 
            // HelpLabel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.Label);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline))), System.Drawing.GraphicsUnit.Point);
            this.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.Name = "HelpLabel";
            this.Size = new System.Drawing.Size(12, 15);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label Label;
        private System.Windows.Forms.ToolTip ToolTip;
    }
}
