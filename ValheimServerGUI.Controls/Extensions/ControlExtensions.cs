using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ValheimServerGUI.Extensions
{
    public static class ControlExtensions
    {
        public static IEnumerable<TControl> FindAllControls<TControl>(this Control control)
            where TControl : Control
        {
            foreach (var c1 in control.Controls.OfType<Control>())
            {
                if (c1 is TControl typed)
                {
                    yield return typed;
                }

                foreach (var c2 in c1.FindAllControls<TControl>())
                {
                    yield return c2;
                }
            }
        }
    }
}
