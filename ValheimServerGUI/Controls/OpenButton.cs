using System;
using System.ComponentModel;
using System.Windows.Forms;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.Forms
{
    public partial class OpenButton : UserControl
    {
        [Browsable(false)]
        public Func<string> PathFunction { get; set; }

        public string HelpText
        {
            get => IconButton.HelpText;
            set => IconButton.HelpText = value;
        }

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
