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
