﻿using System;
using System.Collections;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Windows.Forms;
using ValheimServerGUI.Properties;

namespace ValheimServerGUI.Tools
{
    public static class WinFormsExtensions
    {
        #region Control extensions

        public static Action<TArgs> BuildActionHandler<TArgs>(this Control control, Action<TArgs> action)
        {
            var eventHandler = control.BuildEventHandler(action);
            return (args) => eventHandler(null, args);
        }

        public static Action BuildActionHandler(this Control control, Action action)
        {
            var eventHandler = control.BuildEventHandler(action);
            return () => eventHandler(null, null);
        }

        public static EventHandler<TArgs> BuildEventHandler<TArgs>(this Control control, Action<TArgs> action)
        {
            return (sender, args) =>
            {
                if (control.IsDisposed) return;

                if (control.InvokeRequired)
                {
                    control.BeginInvoke(action, new object[] { args });
                    return;
                }

                action(args);
            };
        }

        public static EventHandler BuildEventHandler(this Control control, Action action)
        {
            return (sender, args) =>
            {
                if (control.IsDisposed) return;

                // This technique allows cross-thread access to UI controls
                // See here: https://stackoverflow.com/questions/519233/writing-to-a-textbox-from-another-thread
                if (control.InvokeRequired)
                {
                    control.BeginInvoke(action);
                    return;
                }

                action();
            };
        }

        // (jb, 3/25/23) Commenting out these extensions because I'm not using them.
        //public static EventHandler<TArgs> BuildEventHandlerAsync<TArgs>(this Control control, Func<TArgs, Task> taskFunc, int taskDelay = 0)
        //{
        //    return async (sender, args) =>
        //    {
        //        if (taskDelay > 0) await Task.Delay(taskDelay);

        //        await Task.Run(() =>
        //        {
        //            if (control.IsDisposed) return;

        //            if (control.InvokeRequired)
        //            {
        //                control.BeginInvoke(new Func<TArgs, Task>(taskFunc), new object[] { args });
        //                return;
        //            }

        //            taskFunc(args);
        //        });
        //    };
        //}

        //public static EventHandler BuildEventHandlerAsync(this Control control, Func<Task> taskFunc, int taskDelay = 0)
        //{
        //    return async (sender, args) =>
        //    {
        //        if (taskDelay > 0) await Task.Delay(taskDelay);

        //        await Task.Run(() =>
        //        {
        //            if (control.IsDisposed) return;

        //            if (control.InvokeRequired)
        //            {
        //                control.BeginInvoke(new Func<Task>(taskFunc));
        //                return;
        //            }

        //            taskFunc();
        //        });
        //    };
        //}

        /// <summary>
        /// (jb, 5/9/21) For some reason, you cannot set a Form's icon from a Resource in the Designer, so I've been setting it
        /// using the file browser. However, I think this might be causing an issue when publishing the application as a trimmed
        /// single-file executable - some users are encountering errors when trying to load *some image* on startup, and I think
        /// this might be it.
        /// </summary>
        public static void AddApplicationIcon(this Form form)
        {
            form.Icon = Resources.ApplicationIcon;
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
                .GetResourceSet(CultureInfo.InvariantCulture, true, true)
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
