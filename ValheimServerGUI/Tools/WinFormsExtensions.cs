using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Windows.Forms;

namespace ValheimServerGUI.Tools
{
    public static class WinFormsExtensions
    {
        #region TextBox extensions

        public static void AppendLine(this TextBox textBox, string line)
        {
            if (textBox.Text.Length == 0)
            {
                textBox.Text = line;
            }
            else
            {
                textBox.AppendText(Environment.NewLine + line);
            }
        }

        #endregion

        #region ListView extensions

        public static IEnumerable<ListViewItem> FindRowsWithColumnValue(this ListView listView, int colIndex, object value)
        {
            var text = value?.ToString();
            var matches = new List<ListViewItem>();

            if (colIndex == 0)
            {
                // The base ListViewItem's Text represents the text of the first column
                foreach (var item in listView.Items.Cast<ListViewItem>())
                {
                    if (item.Text == text)
                    {
                        matches.Add(item);
                    }
                }
            }
            else if (colIndex > 0)
            {
                // Columns 2, 3, etc. in the ListView are stored in the SubItems
                foreach (var item in listView.Items.Cast<ListViewItem>())
                {
                    if (!item.TryGetSubItem(colIndex, out var subItem)) continue;

                    if (subItem.Text == text)
                    {
                        matches.Add(item);
                    }
                }
            }

            return matches;
        }

        public static ListViewItem AddRowWithColumnValues(this ListView listView, params object[] values)
        {
            ListViewItem item;

            if (values == null || values.Length == 0)
            {
                item = new ListViewItem();
            }
            else
            {
                item = new ListViewItem(values[0].ToString());

                foreach (var value in values.Skip(1))
                {
                    item.SubItems.Add(value?.ToString());
                }
            }

            listView.Items.Add(item);
            return item;
        }

        #endregion

        #region ListViewItem extensions

        public static string GetColumnText(this ListViewItem listViewItem, int colIndex)
        {
            string text = null;

            if (colIndex == 0)
            {
                text = listViewItem.Text;
            }
            else if (colIndex > 0)
            {
                // Note: ListView.Text is the same as ListView.SubItems[0].Text, through some magic
                if (listViewItem.TryGetSubItem(colIndex, out var subItem))
                {
                    text = subItem.Text;
                }
            }

            return text;
        }

        public static string SetColumnText(this ListViewItem listViewItem, int colIndex, object value)
        {
            string text = value?.ToString();

            if (colIndex == 0)
            {
                listViewItem.Text = text;
            }
            else if (colIndex > 0)
            {
                // This will not create new columns, only modify existing ones, which is fine for now
                if (listViewItem.TryGetSubItem(colIndex, out var subItem))
                {
                    subItem.Text = text;
                }
            }

            return text;
        }

        // Important note about ListView:
        // When you do new ListViewItem(text), it automatically gets a SubItem with the same text
        // Not sure if it's just the "Details" LV type
        public static bool TryGetSubItem(this ListViewItem listViewItem, int colIndex, out ListViewItem.ListViewSubItem subItem)
        {
            if (listViewItem.SubItems == null || colIndex >= listViewItem.SubItems.Count)
            {
                subItem = null;
                return false;
            }

            subItem = listViewItem.SubItems[colIndex];
            return true;
        }

        #endregion

        #region ImageList extensions

        public static void AddImagesFromResourceFile(this ImageList list, Type resourcesType)
        {
            var resourceImages = new ResourceManager(resourcesType)
                .GetResourceSet(CultureInfo.CurrentUICulture, true, true)
                .Cast<DictionaryEntry>()
                .Where(de => de.Key != null && de.Value != null && typeof(Image).IsAssignableFrom(de.Value.GetType()))
                .ToDictionary(de => de.Key.ToString(), de => de.Value as Image);
            
            foreach (var (key, image) in resourceImages)
            {
                // For now, only add images that match the list size exactly
                if (image.Size != list.ImageSize) continue;

                list.Images.Add(key, image);
            }
        }

        #endregion
    }
}
