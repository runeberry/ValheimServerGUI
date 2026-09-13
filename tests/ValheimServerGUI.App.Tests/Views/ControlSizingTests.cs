using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace ValheimServerGUI.App.Tests.Views;

// Guards the compact 12px check box / radio circle. Fluent hard-codes these parts' size as local values in
// the template (which an app Style can't override), so they're clamped with Max/Min in Compact.axaml — this
// pins that the clamp still bites (a regression would render Fluent's default 20px box).
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
        var cb = Realize(new CheckBox { Content = "x", IsChecked = true });
        var box = cb.GetVisualDescendants().OfType<Border>().First(b => b.Name == "NormalRectangle");
        Assert.Equal(12, box.Bounds.Width, 0);
        Assert.Equal(12, box.Bounds.Height, 0);
    }

    [AvaloniaFact]
    public void RadioButton_circle_is_12px()
    {
        var rb = Realize(new RadioButton { Content = "y", IsChecked = true });
        var circle = rb.GetVisualDescendants().OfType<Ellipse>().First(e => e.Name == "OuterEllipse");
        Assert.Equal(12, circle.Bounds.Width, 0);
        Assert.Equal(12, circle.Bounds.Height, 0);
    }
}
