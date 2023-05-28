using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace ValheimServerGUI.Forms
{
    public partial class AddRemoveListField : UserControl, IFormField<string>
    {
        #region IFormField implementation

        public string Value
        {
            get => SelectListField.Value;
            set => SelectListField.Value = value;
        }

        public string LabelText
        {
            get => SelectListField.LabelText;
            set => SelectListField.LabelText = value;
        }

        [Editor("System.ComponentModel.Design.MultilineStringEditor", "System.Drawing.Design.UITypeEditor")]
        public string HelpText
        {
            get => SelectListField.HelpText;
            set => SelectListField.HelpText = value;
        }

        public event EventHandler<string> ValueChanged;

        #endregion

        public event Action ItemsChanged;

        /// <summary>
        /// This behavior is called whenever the Add button is clicked.
        /// It must return the value that will be added to the list.
        /// 
        /// If this function is not specified, then nothing will happen when
        /// the Add button is clicked.
        /// </summary>
        [Browsable(false)]
        public Func<string> AddFunction { get; set; }

        /// <summary>
        /// This optional behavior is called whenever the Remove button is clicked.
        /// If specified, it must return true if the value should be removed, or false otherwise.
        /// </summary>
        [Browsable(false)]
        public Func<string, bool> RemoveFunction { get; set; }

        /// <summary>
        /// This behavior is called whenever the Edit button is clicked.
        /// It must return the edited string value to be placed at the
        /// selected index in the list.
        /// 
        /// If this function is not specified, then nothing will happen when
        /// the Edit button is clicked.
        /// </summary>
        [Browsable(false)]
        public Func<string, string> EditFunction { get; set; }

        public bool AllowDuplicates { get; set; }

        private bool _isAddEnabled = true;
        public bool AddEnabled
        {
            get => _isAddEnabled;
            set
            {
                if (value == _isAddEnabled) return;
                _isAddEnabled = value;
                RefreshButtonState();
            }
        }

        private bool _isEditEnabled = true;
        public bool EditEnabled
        {
            get => _isAddEnabled;
            set
            {
                if (value == _isEditEnabled) return;
                _isEditEnabled = value;
                RefreshButtonState();
            }
        }

        private bool _isRemoveEnabled = true;
        public bool RemoveEnabled
        {
            get => _isAddEnabled;
            set
            {
                if (value == _isRemoveEnabled) return;
                _isRemoveEnabled = value;
                RefreshButtonState();
            }
        }

        public AddRemoveListField()
        {
            InitializeComponent();

            SelectListField.ValueChanged += SelectListField_ValueChanged;
            SelectListField.ItemsChanged += SelectListField_ItemsChanged;

            AddButton.Click += AddButton_Click;
            EditButton.Click += EditButton_Click;
            RemoveButton.Click += RemoveButton_Click;

            RefreshButtonState();
        }

        private void SelectListField_ValueChanged(object sender, string selectedItem)
        {
            RefreshButtonState();
            ValueChanged?.Invoke(sender, selectedItem);
        }

        private void SelectListField_ItemsChanged()
        {
            ItemsChanged?.Invoke();
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            if (AddFunction == null) return;

            var itemToAdd = AddFunction.Invoke();

            if (AllowDuplicates)
            {
                SelectListField.AddItem(itemToAdd);
            }
            else
            {
                SelectListField.AddDistinctItem(itemToAdd);
            }
        }

        private void EditButton_Click(object sender, EventArgs e)
        {
            if (EditFunction == null) return;

            var selectedItem = Value;
            if (selectedItem == null) return;

            var editedItem = EditFunction.Invoke(selectedItem);
            SelectListField.SetItem(SelectListField.SelectedIndex, editedItem);
        }

        private void RemoveButton_Click(object sender, EventArgs e)
        {
            var selectedItem = Value;
            if (selectedItem == null) return;

            var doRemove = RemoveFunction?.Invoke(selectedItem) ?? true;
            if (doRemove)
            {
                SelectListField.RemoveSelectedItem();
            }
        }

        private void RefreshButtonState()
        {
            var selectedItem = SelectListField.Value;

            AddButton.Enabled = AddEnabled;

            if (selectedItem != null)
            {
                EditButton.Enabled = EditEnabled;
                RemoveButton.Enabled = RemoveEnabled;
            }
            else
            {
                EditButton.Enabled = false;
                RemoveButton.Enabled = false;
            }
        }
    }
}
