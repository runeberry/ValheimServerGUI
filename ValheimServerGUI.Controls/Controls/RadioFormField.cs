using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using ValheimServerGUI.Extensions;

namespace ValheimServerGUI.Controls
{
    public partial class RadioFormField : UserControl, IFormField<bool>
    {
        #region IFormField implementation

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
                this.ValueChanged?.Invoke(this, value);
            }
        }

        public event EventHandler<bool> ValueChanged;

        #endregion

        public string GroupName { get; set; }

        public IReadOnlyList<RadioFormField> RadioGroup { get; private set; }

        public RadioFormField()
        {
            InitializeComponent();
            
            this.RadioButton.CheckedChanged += RadioButton_Changed;
        }

        public void RefreshRadioGroup()
        {
            this.RadioGroup = this.FindForm()
                .FindAllControls<RadioFormField>()
                .Where(r => r.GroupName == this.GroupName)
                .ToList();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (this.Visible && this.RadioGroup == null)
            {
                this.RefreshRadioGroup();
            }
        }

        private void RadioButton_Changed(object sender, EventArgs e)
        {
            this.ValueChanged?.Invoke(this, this.Value);

            if (this.Value && this.RadioGroup != null)
            {
                foreach (var other in this.RadioGroup.Where(r => r != this))
                {
                    other.Value = false;
                }
            }
        }
    }
}
