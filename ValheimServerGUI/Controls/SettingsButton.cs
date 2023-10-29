using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace ValheimServerGUI.Controls
{
    public partial class SettingsButton : UserControl
    {
        [Browsable(false)]
        public Action ClickFunction { get; set; }

        public string HelpText
        {
            get => IconButton.HelpText;
            set => IconButton.HelpText = value;
        }

        public SettingsButton()
        {
            InitializeComponent();

            IconButton.IconClicked = OnIconClicked;
        }

        private bool OnIconClicked()
        {
            if (ClickFunction == null) return false;

            ClickFunction.Invoke();
            return true;
        }
    }
}
