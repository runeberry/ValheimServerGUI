namespace ValheimServerGUI.Forms
{
    partial class EditButton
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
            this.IconButton = new ValheimServerGUI.Controls.IconButton();
            this.SuspendLayout();
            // 
            // IconButton
            // 
            this.IconButton.ConfirmImage = global::ValheimServerGUI.Properties.Resources.StatusOK_16x;
            this.IconButton.HelpText = "Edit";
            this.IconButton.IconClicked = null;
            this.IconButton.Image = global::ValheimServerGUI.Properties.Resources.Edit_16x;
            this.IconButton.Location = new System.Drawing.Point(0, 0);
            this.IconButton.Name = "IconButton";
            this.IconButton.Size = new System.Drawing.Size(16, 16);
            this.IconButton.TabIndex = 0;
            // 
            // EditButton
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.IconButton);
            this.Name = "EditButton";
            this.Size = new System.Drawing.Size(16, 16);
            this.ResumeLayout(false);

        }

        #endregion

        private ValheimServerGUI.Controls.IconButton IconButton;
    }
}
