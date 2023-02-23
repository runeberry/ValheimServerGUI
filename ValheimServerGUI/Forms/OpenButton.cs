using System;
using System.Windows.Forms;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.Forms
{
    public partial class OpenButton : UserControl
    {
        public Func<string> PathFunction { get; set; }

        public OpenButton()
        {
            InitializeComponent();

            PictureBox.Click += this.BuildEventHandler(PictureBox_Click);
        }

        private void PictureBox_Click()
        {
            try
            {
                var path = PathFunction?.Invoke();
                if (string.IsNullOrWhiteSpace(path)) return;

                OpenHelper.OpenDirectory(path);
            }
            catch
            {
                // Suppress all errors
            }
        }
    }
}
