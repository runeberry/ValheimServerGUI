using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace ValheimServerGUI.Controls
{
    public partial class RadioFormField : UserControl, IFormField<bool>
    {
        public event EventHandler<bool> ValueChanged;

        public string LabelText
        {
            get => this.RadioButton.Text;
            set => this.RadioButton.Text = value;
        }

        [Editor("System.ComponentModel.Design.MultilineStringEditor", "System.Drawing.Design.UITypeEditor")]
        public string HelpText
        {
            get => this.HelpLabel.Text;
            set => this.HelpLabel.Text = value;
        }

        public bool Value
        {
            get => this.RadioButton.Checked;
            set
            {
                if (value == this.Value) return;
                this.RadioButton.Checked = value;
            }
        }

        public RadioFormField()
        {
            InitializeComponent();

            this.RadioButton.CheckedChanged += RadioButton_Changed;
        }

        private void RadioButton_Changed(object sender, EventArgs e)
        {
            this.ValueChanged?.Invoke(this, this.Value);
        }
    }
}
