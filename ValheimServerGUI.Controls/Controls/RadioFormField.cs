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
            get => RadioButton.Text;
            set => RadioButton.Text = value;
        }

        [Editor("System.ComponentModel.Design.MultilineStringEditor", "System.Drawing.Design.UITypeEditor")]
        public string HelpText
        {
            get => HelpLabel.Text;
            set => HelpLabel.Text = value;
        }

        public bool Value
        {
            get => RadioButton.Checked;
            set
            {
                if (value == Value) return;

                RadioButton.Checked = value;
                ValueChanged?.Invoke(this, value);
            }
        }

        public event EventHandler<bool> ValueChanged;

        #endregion

        public string GroupName { get; set; }

        public IReadOnlyList<RadioFormField> RadioGroup { get; private set; }

        public RadioFormField()
        {
            InitializeComponent();

            RadioButton.CheckedChanged += RadioButton_Changed;
        }

        public void RefreshRadioGroup()
        {
            RadioGroup = FindForm()
                .FindAllControls<RadioFormField>()
                .Where(r => r.GroupName == GroupName)
                .ToList();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (Visible && RadioGroup == null)
            {
                RefreshRadioGroup();
            }
        }

        private void RadioButton_Changed(object sender, EventArgs e)
        {
            ValueChanged?.Invoke(this, Value);

            if (Value && RadioGroup != null)
            {
                foreach (var other in RadioGroup.Where(r => r != this))
                {
                    other.Value = false;
                }
            }
        }
    }
}
