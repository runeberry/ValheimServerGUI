using System;
using System.Collections;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ValheimServerGUI.Tools
{
    public static class WinFormsExtensions
    {
        #region Control extensions

        public static EventHandler<TArgs> BuildEventHandler<TArgs>(this Control control, Action<TArgs> action)
        {
            return (sender, args) =>
            {
                if (control.InvokeRequired)
                {
                    control.Invoke(new Action<TArgs>(action), new object[] { args });
                    return;
                }

                action(args);
            };
        }

        public static EventHandler BuildEventHandler(this Control control, Action action)
        {
            return (sender, args) =>
            {
                // This technique allows cross-thread access to UI controls
                // See here: https://stackoverflow.com/questions/519233/writing-to-a-textbox-from-another-thread
                if (control.InvokeRequired)
                {
                    control.Invoke(action);
                    return;
                }

                action();
            };
        }

        public static EventHandler<TArgs> BuildEventHandlerAsync<TArgs>(this Control control, Func<TArgs, Task> taskFunc, int taskDelay = 0)
        {
            return async (sender, args) =>
            {
                if (control.InvokeRequired)
                {
                    control.Invoke(new Func<TArgs, Task>(taskFunc), new object[] { args });
                    return;
                }

                if (taskDelay > 0) await Task.Delay(taskDelay);
                await Task.Run(() => taskFunc(args));
            };
        }

        public static EventHandler BuildEventHandlerAsync(this Control control, Func<Task> taskFunc, int taskDelay = 0)
        {
            return async (sender, args) =>
            {
                if (control.InvokeRequired)
                {
                    control.Invoke(new Func<Task>(taskFunc));
                    return;
                }

                if (taskDelay > 0) await Task.Delay(taskDelay);
                await Task.Run(() => taskFunc());
            };
        }

        #endregion

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
