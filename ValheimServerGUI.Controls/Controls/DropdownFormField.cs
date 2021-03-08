using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace ValheimServerGUI.Controls
{
    public partial class DropdownFormField : UserControl, IFormField<string>
    {
        public string EmptyText { get; set; } = "";

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
                if (value == null)
                {
                    this.ComboBox.SelectedItem = value;
                    return;
                }

                if (this.DataSource.Contains(value))
                {
                    // Only allow value dropdown items to be set
                    this.ComboBox.SelectedItem = value;
                }
            }
        }

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
        }
    }
}
