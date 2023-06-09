using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace ValheimServerGUI.Forms
{
    public partial class EditButton : UserControl
    {
        [Browsable(false)]
        public Action EditFunction { get; set; }

        public string HelpText
        {
            get => IconButton.HelpText;
            set => IconButton.HelpText = value;
        }

        public EditButton()
        {
            InitializeComponent();

            IconButton.IconClicked = OnIconClicked;
        }

        private bool OnIconClicked()
        {
            if (EditFunction == null) return false;

            EditFunction.Invoke();
            return true;
        }
    }
}
