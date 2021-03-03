using System.ComponentModel;
using System.ComponentModel.Design;
using System.Windows.Forms;

namespace ValheimServerGUI.Forms.Controls
{
    [Designer("System.Windows.Forms.Design.ParentControlDesigner, System.Design", typeof(IDesigner))]
    public partial class TextFormField : UserControl
    {
        private const char PasswordChar = '●';
        private const char PasswordCharDisabled = '\0';

        public string LabelText
        {
            get => this.Label.Text;
            set => this.Label.Text = value;
        }

        public string Value
        {
            get => this.TextBox.Text;
            set => this.TextBox.Text = value;
        }

        public bool HideValue
        {
            get => this.TextBox.PasswordChar != PasswordCharDisabled;
            set => this.TextBox.PasswordChar = value ? PasswordChar : PasswordCharDisabled;
        }

        public TextFormField()
        {
            InitializeComponent();
        }
    }
}
