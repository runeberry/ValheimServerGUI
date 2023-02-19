using System;
using System.Drawing;
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

            PictureBox.Click += this.BuildEventHandler(PictureBox_Click);
            Timer.Tick += this.BuildEventHandler(Timer_Tick);
        }

        private void ShowConfirm()
        {
            if (IsLocked) return;

            SetImage(Resources.StatusOK_16x, Cursors.Default);
            Timer.Start();
            IsLocked = true;
        }

        private void Timer_Tick()
        {
            if (!IsLocked) return;

            SetImage(Resources.Copy_16x, Cursors.Hand);
            Timer.Stop();
            IsLocked = false;
        }

        private void SetImage(Image image, Cursor cursor)
        {
            PictureBox.Image = image;
            PictureBox.Cursor = cursor;
            PictureBox.Refresh();
            PictureBox.Visible = true;
        }

        private void PictureBox_Click()
        {
            if (IsLocked) return;

            try
            {
                var str = CopyFunction?.Invoke();
                if (string.IsNullOrEmpty(str)) return;

                Clipboard.SetText(str);
                ShowConfirm();
            }
            catch
            {
                // Suppress all errors
            }
        }
    }
}
