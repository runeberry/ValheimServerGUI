using System.Windows.Forms;

namespace ValheimServerGUI.Controls
{
    public partial class CheckboxFormField : UserControl, IFormField<bool>
    {
        public string LabelText
        {
            get => this.CheckBox.Text;
            set => this.CheckBox.Text = value;
        }

        public bool Value
        {
            get => this.CheckBox.Checked;
            set => this.CheckBox.Checked = value;
        }

        public CheckboxFormField()
        {
            InitializeComponent();
        }
    }
}
