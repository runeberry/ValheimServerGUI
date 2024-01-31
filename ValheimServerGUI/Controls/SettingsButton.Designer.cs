namespace ValheimServerGUI.Controls
{
    partial class SettingsButton
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
            IconButton = new IconButton();
            SuspendLayout();
            // 
            // IconButton
            // 
            IconButton.ConfirmImage = null;
            IconButton.HelpText = "";
            IconButton.IconClicked = null;
            IconButton.Image = Properties.Resources.Settings_16x;
            IconButton.Location = new System.Drawing.Point(0, 0);
            IconButton.Name = "IconButton";
            IconButton.Size = new System.Drawing.Size(16, 16);
            IconButton.TabIndex = 0;
            // 
            // SettingsButton
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(IconButton);
            Name = "SettingsButton";
            Size = new System.Drawing.Size(16, 16);
            ResumeLayout(false);
        }

        #endregion

        private IconButton IconButton;
    }
}
