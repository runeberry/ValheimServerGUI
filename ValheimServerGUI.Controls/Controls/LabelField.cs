using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ValheimServerGUI.Controls
{
    public partial class LabelField : UserControl, IFormField<string>
    {
        #region IFormField implementation

        public string LabelText
        {
            get => FormLabel.Text;
            set => FormLabel.Text = value;
        }

        [Editor("System.ComponentModel.Design.MultilineStringEditor", "System.Drawing.Design.UITypeEditor")]
        public string HelpText
        {
            get => HelpLabel.Text;
            set => HelpLabel.Text = value;
        }

        public string Value
        {
            get => ValueLabel.Text;
            set
            {
                if (ValueLabel.Text == value) return;

                ValueLabel.Text = value;
                ValueChanged?.Invoke(this, value);
            }
        }

        public event EventHandler<string> ValueChanged;

        #endregion

        private double _lsr = 0.5;
        public double LabelSplitRatio
        {
            get => _lsr;
            set
            {
                _lsr = value;
                ResizeLabels();
            }
        }

        public ContentAlignment LabelTextAlign
        {
            get => FormLabel.TextAlign;
            set => FormLabel.TextAlign = value;
        }

        public ContentAlignment ValueTextAlign
        {
            get => ValueLabel.TextAlign;
            set => ValueLabel.TextAlign = value;
        }

        public LabelField()
        {
            InitializeComponent();

            ResizeLabels();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            ResizeLabels();
        }

        private void ResizeLabels()
        {
            var fillWidth = Width - FormLabel.Location.X - HelpLabel.Width;
            var split = Math.Min(1.0, Math.Max(0.0, LabelSplitRatio));

            // The Label and Value should each fill up 50% of the available space
            FormLabel.Width = (int)(fillWidth * split);
            ValueLabel.Width = (int)(fillWidth * (1 - split));

            // The Value should always be anchored directly to the right of the Label
            var locX = FormLabel.Location.X + FormLabel.Width;
            ValueLabel.Location = new Point(locX, ValueLabel.Location.Y);
        }
    }
}
