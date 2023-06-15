using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace ValheimServerGUI.Forms
{
    public partial class CopyButton : UserControl
    {
        [Browsable(false)]
        public Func<string> CopyFunction { get; set; }

        public string HelpText
        {
            get => IconButton.HelpText;
            set => IconButton.HelpText = value;
        }

        public CopyButton()
        {
            InitializeComponent();

            IconButton.IconClicked = OnIconClicked;
        }

        private bool OnIconClicked()
        {
            var str = CopyFunction?.Invoke();
            if (string.IsNullOrEmpty(str)) return false;

            Clipboard.SetText(str);
            return true;
        }
    }
}
