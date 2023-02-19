using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace ValheimServerGUI.Controls
{
    public partial class NumericFormField : UserControl, IFormField<int>
    {
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

        public int Value
        {
            get => GetValidatedValue((int)NumericUpDown.Value);
            set
            {
                var validValue = GetValidatedValue(value);
                if (validValue != NumericUpDown.Value)
                {
                    NumericUpDown.Value = validValue;
                    ValueChanged?.Invoke(this, Value);
                }
            }
        }

        public event EventHandler<int> ValueChanged;

        #endregion

        public int Minimum
        {
            get => (int)NumericUpDown.Minimum;
            set => NumericUpDown.Minimum = value;
        }

        public int Maximum
        {
            get => (int)NumericUpDown.Maximum;
            set => NumericUpDown.Maximum = value;
        }

        public NumericFormField()
        {
            InitializeComponent();

            NumericUpDown.ValueChanged += OnValueChanged;
        }

        private void OnValueChanged(object sender, EventArgs args)
        {
            ValueChanged?.Invoke(this, Value);
        }

        private int GetValidatedValue(int val)
        {
            if (val < NumericUpDown.Minimum)
            {
                val = (int)NumericUpDown.Minimum;
            }
            else if (val > NumericUpDown.Maximum)
            {
                val = (int)NumericUpDown.Maximum;
            }

            return val;
        }
    }
}
