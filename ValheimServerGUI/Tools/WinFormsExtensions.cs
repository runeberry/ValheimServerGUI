using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ValheimServerGUI.Tools
{
    public static class WinFormsExtensions
    {
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
    }
}
