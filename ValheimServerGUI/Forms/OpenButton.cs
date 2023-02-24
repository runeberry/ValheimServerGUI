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

            IconButton.IconClicked = OnIconClicked;
        }

        private bool OnIconClicked()
        {
            var path = PathFunction?.Invoke();
            if (string.IsNullOrWhiteSpace(path)) return false;

            OpenHelper.OpenDirectory(path);
            return true;
        }
    }
}
