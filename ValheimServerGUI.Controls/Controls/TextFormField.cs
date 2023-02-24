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
            get => Label.Text;
            set => Label.Text = value;
        }

        [Editor("System.ComponentModel.Design.MultilineStringEditor", "System.Drawing.Design.UITypeEditor")]
        public string HelpText
        {
            get => HelpLabel.Text;
            set => HelpLabel.Text = value;
        }

        public string Value
        {
            get => TextBox.Text;
            set
            {
                if (TextBox.Text == value) return;

                TextBox.Text = value;
                ValueChanged?.Invoke(this, value);
            }
        }

        public event EventHandler<string> ValueChanged;

        #endregion

        public bool HideValue
        {
            get => TextBox.PasswordChar != PasswordCharDisabled;
            set => TextBox.PasswordChar = value ? PasswordChar : PasswordCharDisabled;
        }

        public int MaxLength
        {
            get => TextBox.MaxLength;
            set => TextBox.MaxLength = value;
        }

        public bool Multiline
        {
            get => TextBox.Multiline;
            set => TextBox.Multiline = value;
        }

        public TextFormField()
        {
            InitializeComponent();

            TextBox.TextChanged += OnTextChanged;
        }

        public void SelectAll()
        {
            TextBox.SelectAll();
        }

        private void OnTextChanged(object sender, EventArgs args)
        {
            ValueChanged?.Invoke(this, Value);
        }
    }
}
