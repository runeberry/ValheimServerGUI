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
            get => CheckBox.Text;
            set => CheckBox.Text = value;
        }

        [Editor("System.ComponentModel.Design.MultilineStringEditor", "System.Drawing.Design.UITypeEditor")]
        public string HelpText
        {
            get => HelpLabel.Text;
            set => HelpLabel.Text = value;
        }

        public bool Value
        {
            get => CheckBox.Checked;
            set
            {
                if (value == Value) return;

                CheckBox.Checked = value;
                ValueChanged?.Invoke(this, value);
            }
        }

        public event EventHandler<bool> ValueChanged;

        #endregion

        public CheckboxFormField()
        {
            InitializeComponent();

            CheckBox.CheckedChanged += CheckBox_Changed;
        }

        private void CheckBox_Changed(object sender, EventArgs e)
        {
            ValueChanged?.Invoke(this, Value);
        }
    }
}
