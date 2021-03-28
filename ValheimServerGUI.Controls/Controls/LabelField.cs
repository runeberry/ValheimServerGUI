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
            get => this.FormLabel.Text;
            set => this.FormLabel.Text = value;
        }

        [Editor("System.ComponentModel.Design.MultilineStringEditor", "System.Drawing.Design.UITypeEditor")]
        public string HelpText
        {
            get => this.HelpLabel.Text;
            set => this.HelpLabel.Text = value;
        }

        public string Value
        {
            get => this.ValueLabel.Text;
            set
            {
                if (this.ValueLabel.Text == value) return;

                this.ValueLabel.Text = value;
                this.ValueChanged?.Invoke(this, value);
            }
        }

        public event EventHandler<string> ValueChanged;

        #endregion

        private double _lsr = 0.5;
        public double LabelSplitRatio
        {
            get => this._lsr;
            set
            {
                this._lsr = value;
                this.ResizeLabels();
            }
        }

        public ContentAlignment LabelTextAlign
        {
            get => this.FormLabel.TextAlign;
            set => this.FormLabel.TextAlign = value;
        }

        public ContentAlignment ValueTextAlign
        {
            get => this.ValueLabel.TextAlign;
            set => this.ValueLabel.TextAlign = value;
        }

        public LabelField()
        {
            InitializeComponent();

            this.ResizeLabels();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            this.ResizeLabels();
        }

        private void ResizeLabels()
        {
            var fillWidth = this.Width - this.FormLabel.Location.X - this.HelpLabel.Width;
            var split = Math.Min(1.0, Math.Max(0.0, this.LabelSplitRatio));

            // The Label and Value should each fill up 50% of the available space
            this.FormLabel.Width = (int)(fillWidth * split);
            this.ValueLabel.Width = (int)(fillWidth * (1-split));

            // The Value should always be anchored directly to the right of the Label
            var locX = this.FormLabel.Location.X + this.FormLabel.Width;
            this.ValueLabel.Location = new Point(locX, this.ValueLabel.Location.Y);
    }
    }
}
