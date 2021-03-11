using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using ValheimServerGUI.Extensions;

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

        public string GroupName { get; set; }

        public RadioFormField()
        {
            InitializeComponent();
            
            this.RadioButton.CheckedChanged += RadioButton_Changed;
        }

        private void RadioButton_Changed(object sender, EventArgs e)
        {
            this.ValueChanged?.Invoke(this, this.Value);

            if (this.Value)
            {
                // Find any other radio buttons in this group that are checked and uncheck them
                // todo: optimize by caching the list of related controls on form load
                var others = this.FindForm()
                    .FindAllControls<RadioFormField>()
                    .Where(r => r.GroupName == this.GroupName && r.Value && r != this);

                foreach (var other in others)
                {
                    other.Value = false;
                }
            }
        }
    }
}
