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
            get => ToolTip.GetToolTip(Label);
            set
            {
                ToolTip.SetToolTip(Label, value);
                Label.Visible = !string.IsNullOrWhiteSpace(value);
            }
        }

        public HelpLabel()
        {
            InitializeComponent();
        }
    }
}
