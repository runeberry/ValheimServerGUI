using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ValheimServerGUI.App.Controls;
using Xunit;

namespace ValheimServerGUI.App.Tests.Views;

// Help text reads as body text: only the "?" glyph is bold, and a tooltip inherits the font of the control it hangs on.
public class HelpTooltipWeightTests
{
    [AvaloniaFact]
    public void Help_tooltip_text_is_not_bold()
    {
        var field = new TextFormField { LabelText = "Name", HelpText = "What this field does." };
        var window = new Window { Content = field, Width = 400, Height = 200 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var host = field.GetVisualDescendants().OfType<HelpLabel>().Single()
            .GetSelfAndVisualDescendants().OfType<Control>().Single(c => ToolTip.GetTip(c) != null);
        ToolTip.SetIsOpen(host, true);
        Dispatcher.UIThread.RunJobs();

        // The open ToolTip is held in a non-public attached property; there is no public way to reach it.
        var property = (AvaloniaProperty?)typeof(ToolTip)
            .GetField("ToolTipProperty", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);
        Assert.NotNull(property);
        var tip = Assert.IsType<ToolTip>(host.GetValue(property));
        Assert.Equal(FontWeight.Normal, tip.FontWeight);
        window.Close();
    }
}
