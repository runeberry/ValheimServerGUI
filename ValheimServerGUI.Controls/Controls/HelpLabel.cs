using System.ComponentModel;
using System.Windows.Forms;

namespace ValheimServerGUI.Controls
{
    public partial class HelpLabel : UserControl
    {
        [Editor("System.ComponentModel.Design.MultilineStringEditor", "System.Drawing.Design.UITypeEditor")]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override string Text
        {
            get => this.ToolTip.GetToolTip(this.Label);
            set
            {
                this.ToolTip.SetToolTip(this.Label, value);
                this.Label.Visible = !string.IsNullOrWhiteSpace(value);
            }
        }

        public HelpLabel()
        {
            InitializeComponent();
        }
    }
}
