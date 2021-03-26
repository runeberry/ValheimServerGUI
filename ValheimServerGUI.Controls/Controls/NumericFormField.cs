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
            get => this.Label.Text;
            set => this.Label.Text = value;
        }

        [Editor("System.ComponentModel.Design.MultilineStringEditor", "System.Drawing.Design.UITypeEditor")]
        public string HelpText
        {
            get => this.HelpLabel.Text;
            set => this.HelpLabel.Text = value;
        }

        public int Value
        {
            get => (int)this.NumericUpDown.Value;
            set 
            {
                if (this.NumericUpDown.Value == value) return;

                this.NumericUpDown.Value = value;
                this.ValueChanged?.Invoke(this, Value);
            }
        }

        public event EventHandler<int> ValueChanged;

        #endregion

        public int Minimum
        {
            get => (int)this.NumericUpDown.Minimum;
            set => this.NumericUpDown.Minimum = value;
        }

        public int Maximum
        {
            get => (int)this.NumericUpDown.Maximum;
            set => this.NumericUpDown.Maximum = value;
        }

        public NumericFormField()
        {
            InitializeComponent();

            this.NumericUpDown.ValueChanged += this.OnValueChanged;
        }

        private void OnValueChanged(object sender, EventArgs args)
        {
            this.ValueChanged?.Invoke(this, this.Value);
        }
    }
}
