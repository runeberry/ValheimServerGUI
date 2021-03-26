using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace ValheimServerGUI.Controls
{
    public partial class CheckboxFormField : UserControl, IFormField<bool>
    {
        #region IFormField implementation

        public string LabelText
        {
            get => this.CheckBox.Text;
            set => this.CheckBox.Text = value;
        }

        [Editor("System.ComponentModel.Design.MultilineStringEditor", "System.Drawing.Design.UITypeEditor")]
        public string HelpText
        {
            get => this.HelpLabel.Text;
            set => this.HelpLabel.Text = value;
        }

        public bool Value
        {
            get => this.CheckBox.Checked;
            set
            {
                if (value == this.Value) return;

                this.CheckBox.Checked = value;
                this.ValueChanged?.Invoke(this, value);
            }
        }

        public event EventHandler<bool> ValueChanged;

        #endregion

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
