using System;
using System.ComponentModel;
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

        [Editor("System.ComponentModel.Design.MultilineStringEditor", "System.Drawing.Design.UITypeEditor")]
        public string HelpText
        {
            get => this.HelpToolTip.GetToolTip(this.HelpLabel);
            set
            {
                this.HelpToolTip.SetToolTip(this.HelpLabel, value);
                this.HelpLabel.Visible = !string.IsNullOrWhiteSpace(value);
            }
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

            this.CheckBox.CheckedChanged += this.CheckBox_Changed;
        }

        private void CheckBox_Changed(object sender, EventArgs e)
        {
            this.ValueChanged?.Invoke(this, this.Value);
        }
    }
}
