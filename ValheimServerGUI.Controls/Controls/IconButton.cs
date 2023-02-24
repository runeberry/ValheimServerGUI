using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ValheimServerGUI.Controls
{
    public partial class IconButton : UserControl
    {
        public Image Image { get; set; }

        public Image ConfirmImage { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Browsable(false)]
        public Func<bool> IconClicked { get; set; }

        public string HelpText
        {
            get => ToolTip.GetToolTip(PictureBox);
            set => ToolTip.SetToolTip(PictureBox, value);
        }

        private static readonly ToolTip ToolTip = new();
        private bool IsLocked;

        public IconButton()
        {
            InitializeComponent();
            VisibleChanged += OnVisibleChanged;
        }

        private void OnVisibleChanged(object sender, EventArgs e)
        {
            SetImage(Image, Cursors.Hand);

            PictureBox.Click += PictureBox_Click;
            Timer.Tick += Timer_Tick;

            // Only run the first time the control is shown
            VisibleChanged -= OnVisibleChanged;
        }

        protected void PictureBox_Click(object sender, EventArgs args)
        {
            if (IsLocked || IconClicked == null) return;

            try
            {
                if (IconClicked.Invoke())
                {
                    ShowConfirm();
                }
            }
            catch
            {
                // Suppress all errors
            }
        }

        protected void Timer_Tick(object sender, EventArgs args)
        {
            if (!IsLocked) return;

            SetImage(Image, Cursors.Hand);
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

        private void ShowConfirm()
        {
            if (IsLocked || ConfirmImage == null) return;

            SetImage(ConfirmImage, Cursors.Default);
            Timer.Start();
            IsLocked = true;
        }
    }
}
