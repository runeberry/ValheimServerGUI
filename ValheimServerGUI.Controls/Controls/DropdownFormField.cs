using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace ValheimServerGUI.Controls
{
    public partial class DropdownFormField : UserControl, IFormField<string>
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

        public string Value
        {
            get
            {
                var selectedItem = this.ComboBox.SelectedItem?.ToString();

                if (selectedItem == EmptyText)
                {
                    return null;
                }

                return selectedItem;
            }
            set
            {
                if (this.ComboBox.SelectedItem?.ToString() == value) return;

                if (value == null || this.DataSource.Contains(value))
                {
                    // Only allow null or values within DataSource to be set, ignore all others
                    this.ComboBox.SelectedItem = value;
                    this.ValueChanged?.Invoke(this, value);
                }
            }
        }

        public event EventHandler<string> ValueChanged;

        #endregion

        public string EmptyText { get; set; } = "";

        public IEnumerable<string> DataSource
        {
            get
            {
                var dataSource = this.ComboBox.DataSource as IEnumerable<string>;

                if (dataSource == null)
                {
                    dataSource = new List<string>();
                    this.ComboBox.DataSource = dataSource;
                }

                if (dataSource.Count() == 1 && dataSource.Single() == EmptyText)
                {
                    dataSource = dataSource.Take(0);
                }

                return dataSource;
            }
            set
            {
                if (value == null || !value.Any())
                {
                    value = new List<string> { EmptyText };
                }

                this.ComboBox.DataSource = value;
            }
        }

        public bool DropdownEnabled
        {
            get => this.ComboBox.Enabled;
            set => this.ComboBox.Enabled = value;
        }

        public DropdownFormField()
        {
            InitializeComponent();

            this.ComboBox.SelectedIndexChanged += this.OnSelectedIndexChanged;
        }

        private void OnSelectedIndexChanged(object sender, EventArgs args)
        {
            this.ValueChanged?.Invoke(this, this.Value);
        }
    }
}
