using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ValheimServerGUI.App.Controls;
using Xunit;

namespace ValheimServerGUI.App.Tests.Views;

// Guards the compact 12px check box / radio circle owned by the CheckBoxFormField / RadioFormField wrappers.
// The box/circle are our own scoped templates (parts named Box / Ring), so a regression would render at a
// different size or drop the part entirely.
public class ControlSizingTests
{
    private static T Realize<T>(T control) where T : Control
    {
        var window = new Window { Content = control, Width = 200, Height = 120 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.Measure(new Size(200, 120));
        window.Arrange(new Rect(new Size(200, 120)));
        Dispatcher.UIThread.RunJobs();
        return control;
    }

    [AvaloniaFact]
    public void CheckBox_box_is_12px()
    {
        var field = Realize(new CheckBoxFormField { LabelText = "x", Value = true });
        var box = field.GetVisualDescendants().OfType<Border>().First(b => b.Name == "Box");
        Assert.Equal(12, box.Bounds.Width, 0);
        Assert.Equal(12, box.Bounds.Height, 0);
    }

    [AvaloniaFact]
    public void RadioButton_circle_is_12px()
    {
        var field = Realize(new RadioFormField { LabelText = "y", Value = true });
        var ring = field.GetVisualDescendants().OfType<Ellipse>().First(e => e.Name == "Ring");
        Assert.Equal(12, ring.Bounds.Width, 0);
        Assert.Equal(12, ring.Bounds.Height, 0);
    }
}
