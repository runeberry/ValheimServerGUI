namespace ValheimServerGUI.Controls
{
    partial class IconButton
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
            this.Timer = new System.Windows.Forms.Timer(this.components);
            this.PictureBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.PictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // Timer
            // 
            this.Timer.Interval = 2000;
            // 
            // PictureBox
            // 
            this.PictureBox.Cursor = System.Windows.Forms.Cursors.Hand;
            this.PictureBox.InitialImage = null;
            this.PictureBox.Location = new System.Drawing.Point(0, 0);
            this.PictureBox.Name = "PictureBox";
            this.PictureBox.Size = new System.Drawing.Size(16, 16);
            this.PictureBox.TabIndex = 0;
            this.PictureBox.TabStop = false;
            // 
            // IconButton
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.PictureBox);
            this.Name = "IconButton";
            this.Size = new System.Drawing.Size(16, 16);
            ((System.ComponentModel.ISupportInitialize)(this.PictureBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        protected System.Windows.Forms.PictureBox PictureBox;
        protected System.Windows.Forms.Timer Timer;
    }
}
