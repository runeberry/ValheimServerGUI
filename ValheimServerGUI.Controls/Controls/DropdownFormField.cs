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
            get => Label.Text;
            set => Label.Text = value;
        }

        [Editor("System.ComponentModel.Design.MultilineStringEditor", "System.Drawing.Design.UITypeEditor")]
        public string HelpText
        {
            get => HelpLabel.Text;
            set => HelpLabel.Text = value;
        }

        public string Value
        {
            get
            {
                var selectedItem = ComboBox.SelectedItem?.ToString();

                if (selectedItem == EmptyText)
                {
                    return null;
                }

                return selectedItem;
            }
            set
            {
                if (ComboBox.SelectedItem?.ToString() == value) return;

                if (value == null || DataSource.Contains(value))
                {
                    // Only allow null or values within DataSource to be set, ignore all others
                    ComboBox.SelectedItem = value;
                    ValueChanged?.Invoke(this, value);
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
                var dataSource = ComboBox.DataSource as IEnumerable<string>;

                if (dataSource == null)
                {
                    dataSource = new List<string>();
                    ComboBox.DataSource = dataSource;
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

                ComboBox.DataSource = value;
            }
        }

        public bool DropdownEnabled
        {
            get => ComboBox.Enabled;
            set => ComboBox.Enabled = value;
        }

        public DropdownFormField()
        {
            InitializeComponent();

            ComboBox.SelectedIndexChanged += OnSelectedIndexChanged;
        }

        private void OnSelectedIndexChanged(object sender, EventArgs args)
        {
            ValueChanged?.Invoke(this, Value);
        }
    }
}
