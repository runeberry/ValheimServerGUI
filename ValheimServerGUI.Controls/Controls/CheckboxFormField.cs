using System;
using System.Windows.Forms;

namespace ValheimServerGUI.Controls
{
    public partial class CheckboxFormField : UserControl, IFormField<bool>
    {
        public event EventHandler<bool> ValueChanged;

        public string LabelText
        {
            get => this.CheckBox.Text;
            set => this.CheckBox.Text = value;
        }

        public bool Value
        {
            get => this.CheckBox.Checked;
            set
            {
                if (value == this.Value) return;
                this.CheckBox.Checked = value;
            }
        }

        public CheckboxFormField()
        {
            InitializeComponent();

            this.CheckBox.CheckedChanged += new EventHandler(this.CheckBox_Changed);
        }

        private void CheckBox_Changed(object sender, EventArgs e)
        {
            this.ValueChanged?.Invoke(this, this.Value);
        }
    }
}
