using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace ValheimServerGUI.Controls
{
    public partial class SelectListField : UserControl, IFormField<string>
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

        /// <summary>
        /// The string value of the currently selected item, or null if
        /// no item is currently selected.
        /// </summary>
        public string Value
        {
            get
            {
                if (ListBox.SelectedItem == null) return null;
                return ListBox.GetItemText(ListBox.SelectedItem);
            }
            set
            {
                var index = GetItemIndex(value);
                if (index != ListBox.SelectedIndex)
                {
                    ListBox.SelectedIndex = GetItemIndex(value);
                    if (ListBox.SelectedIndex >= 0)
                    {
                        ValueChanged?.Invoke(this, value);
                    }
                    else
                    {
                        ValueChanged?.Invoke(this, null);
                    }
                }
            }
        }

        public event EventHandler<string> ValueChanged;

        #endregion

        public event Action ItemsChanged;

        public int SelectedIndex => ListBox.SelectedIndex;

        public SelectListField()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Adds an item to the end of the list. Returns the index of
        /// the newly-added item, or -1 if it could not be added.
        /// </summary>
        public int AddItem(string item)
        {
            if (!IsValidItem(item)) return -1;

            var index = ListBox.Items.Add(item);
            ItemsChanged?.Invoke();
            return index;
        }

        /// <summary>
        /// Adds an item to the end of the list, only if the item is not
        /// already in the list. Returns the index of the newly-added item,
        /// or the index of the existing item if it was already in the list.
        /// </summary>
        public int AddDistinctItem(string item)
        {
            if (!IsValidItem(item)) return -1;

            var existingIndex = GetItemIndex(item);
            if (existingIndex >= 0) return existingIndex;
            return AddItem(item);
        }

        public void ClearItems()
        {
            if (ListBox.Items.Count == 0) return;

            ListBox.Items.Clear();
            ItemsChanged?.Invoke();
        }

        /// <summary>
        /// Returns the text at the specified index in the list, or null
        /// if the index is out of bounds.
        /// </summary>
        public string GetItem(int index)
        {
            if (!IsValidIndex(index)) return null;
            return ListBox.GetItemText(ListBox.Items[index]);
        }

        /// <summary>
        /// Returns the text of all items in the list.
        /// </summary>
        public IEnumerable<string> GetItems()
        {
            foreach (var itemObj in ListBox.Items)
            {
                if (itemObj == null) continue;
                yield return ListBox.GetItemText(itemObj);
            }
        }

        /// <summary>
        /// Returns the index of the first instance of the specified item in the list.
        /// Returns -1 if the specified item is null, or if it is not found in the list.
        /// </summary>
        public int GetItemIndex(string item)
        {
            if (!IsValidItem(item)) return -1;

            var i = 0;
            foreach (var itemObj in ListBox.Items)
            {
                if (itemObj != null)
                {
                    var itemText = ListBox.GetItemText(itemObj);
                    if (itemText == item) return i;
                }
                i++;
            }

            return -1;
        }

        /// <summary>
        /// Removes the first instance of the specified item from the list.
        /// </summary>
        public void RemoveItem(string item)
        {
            if (!IsValidItem(item)) return;
            RemoveItem(GetItemIndex(item));
        }

        /// <summary>
        /// Removes the item at the specified index. Does nothing if the
        /// specified index is out of range.
        /// </summary>
        /// <param name="index"></param>
        public void RemoveItem(int index)
        {
            if (!IsValidIndex(index)) return;

            ListBox.Items.Remove(index);
            ItemsChanged?.Invoke();
        }

        /// <summary>
        /// Removes the currently selected item from the list. Does nothing
        /// if no item is currently selected.
        /// </summary>
        public void RemoveSelectedItem()
        {
            RemoveItem(ListBox.SelectedIndex);
        }

        /// <summary>
        /// Selects the first item that matches the specified text in the list.
        /// </summary>
        public void SelectItem(string item)
        {
            if (!IsValidItem(item)) return;
            SelectItem(GetItemIndex(item));
        }

        /// <summary>
        /// Selects the item at the specified index in the list.
        /// </summary>
        public void SelectItem(int index)
        {
            if (!IsValidIndex(index)) return;
            ListBox.SetSelected(index, true);
        }

        /// <summary>
        /// Changes the value of the item at the specified index.
        /// If the index < 0, then the item will be prepended to the beginning
        /// of the list. If the index > length, then the item will be
        /// appended to the end of the list.
        /// </summary>
        public void SetItem(int index, string item)
        {
            if (!IsValidIndex(index) || !IsValidItem(item)) return;

            if (index < 0)
            {
                ListBox.Items.Insert(0, item);
            }
            else if (index >= ListBox.Items.Count)
            {
                ListBox.Items.Add(item);
            }
            else
            {
                var existingItem = GetItem(index);
                if (existingItem == item) return;

                ListBox.Items[index] = item;
            }

            ItemsChanged?.Invoke();
        }

        /// <summary>
        /// Overwrites the list with the provided list of items.
        /// Always invokes the <see cref="ItemsChanged"/> event.
        /// </summary>
        public void SetItems(IEnumerable<string> items)
        {
            ListBox.Items.Clear();

            bool anyChanged = false;

            foreach (var item in items)
            {
                if (!IsValidItem(item)) continue;

                anyChanged = true;
                ListBox.Items.Add(item);
            }

            if (anyChanged)
            {
                ItemsChanged?.Invoke();
            }
        }

        private bool IsValidIndex(int index)
        {
            return index >= 0 && index < ListBox.Items.Count;
        }

        private bool IsValidItem(string item)
        {
            return !string.IsNullOrWhiteSpace(item);
        }
    }
}
