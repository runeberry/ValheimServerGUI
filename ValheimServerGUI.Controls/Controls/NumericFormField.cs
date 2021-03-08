using System.ComponentModel;
using System.Windows.Forms;

namespace ValheimServerGUI.Controls
{
    public partial class NumericFormField : UserControl, IFormField<int>
    {
        public string LabelText
        {
            get => this.Label.Text;
            set => this.Label.Text = value;
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

        public int Value
        {
            get => (int)this.NumericUpDown.Value;
            set => this.NumericUpDown.Value = value;
        }

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
        }
    }
}
