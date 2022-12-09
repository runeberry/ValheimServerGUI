using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace ValheimServerGUI.Forms.Controls
{
    public partial class TextFormField : UserControl, IFormField<string>
    {
        private const char PasswordChar = '●';
        private const char PasswordCharDisabled = '\0';

        #region IFormField implementation

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
            set
            {
                if (this.TextBox.Text == value) return;

                this.TextBox.Text = value;
                this.ValueChanged?.Invoke(this, value);
            }
        }

        public event EventHandler<string> ValueChanged;

        #endregion

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

        public bool Multiline
        {
            get => this.TextBox.Multiline;
            set => this.TextBox.Multiline = value;
        }

        public TextFormField()
        {
            InitializeComponent();

            this.TextBox.TextChanged += this.OnTextChanged;
        }

        public void SelectAll()
        {
            this.TextBox.SelectAll();
        }

        private void OnTextChanged(object sender, EventArgs args)
        {
            this.ValueChanged?.Invoke(this, this.Value);
        }
    }
}
