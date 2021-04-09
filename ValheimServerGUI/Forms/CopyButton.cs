using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ValheimServerGUI.Properties;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.Forms
{
    public partial class CopyButton : UserControl
    {
        public Func<string> CopyFunction { get; set; }

        private bool IsLocked;

        public CopyButton()
        {
            InitializeComponent();

            this.PictureBox.Click += this.BuildEventHandler(this.PictureBox_Click);
            this.Timer.Tick += this.BuildEventHandler(this.Timer_Tick);
        }

        private void ShowConfirm()
        {
            if (this.IsLocked) return;

            this.SetImage(Resources.StatusOK_16x, Cursors.Default);
            this.Timer.Start();
            this.IsLocked = true;
        }

        private void Timer_Tick()
        {
            if (!this.IsLocked) return;

            this.SetImage(Resources.Copy_16x, Cursors.Hand);
            this.Timer.Stop();
            this.IsLocked = false;
        }

        private void SetImage(Image image, Cursor cursor)
        {
            this.PictureBox.Image = image;
            this.PictureBox.Cursor = cursor;
            this.PictureBox.Refresh();
            this.PictureBox.Visible = true;
        }

        private void PictureBox_Click()
        {
            if (this.IsLocked) return;

            try
            {
                var str = this.CopyFunction?.Invoke();
                if (string.IsNullOrEmpty(str)) return;

                Clipboard.SetText(str);
                this.ShowConfirm();
            }
            catch
            {
                // Suppress all errors
            }
        }
    }
}
