using System.ComponentModel;
using System.Windows.Forms;

namespace ValheimServerGUI.Forms.Controls
{
    public partial class TextFormField : UserControl, IFormField<string>
    {
        private const char PasswordChar = '●';
        private const char PasswordCharDisabled = '\0';

        public string LabelText
        {
            get => this.Label.Text;
            set => this.Label.Text = value;
        }

        [Editor("System.ComponentModel.Design.MultilineStringEditor", "System.Drawing.Design.UITypeEditor")]
        public string HelpText
        {
            get => this.HelpLabel.Text;
            set => this.HelpLabel.Text = value;
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

        public int MaxLength
        {
            get => this.TextBox.MaxLength;
            set => this.TextBox.MaxLength = value;
        }

        public TextFormField()
        {
            InitializeComponent();
        }
    }
}
