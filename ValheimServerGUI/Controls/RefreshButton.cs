using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace ValheimServerGUI.Forms
{
    public partial class RefreshButton : UserControl
    {
        [Browsable(false)]
        public Action RefreshFunction { get; set; }

        public string HelpText
        {
            get => IconButton.HelpText;
            set => IconButton.HelpText = value;
        }

        public RefreshButton()
        {
            InitializeComponent();

            IconButton.IconClicked = OnIconClicked;
        }

        private bool OnIconClicked()
        {
            if (RefreshFunction == null) return false;

            RefreshFunction.Invoke();
            return true;
        }
    }
}
