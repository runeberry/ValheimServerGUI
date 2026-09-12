using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Xunit;

namespace ValheimServerGUI.App.Tests.Views;

// The compact vertically-stacked NumericUpDown spinner (ported from mochi-paint): the up/down repeat
// buttons stay wired (named PART_Increase/DecreaseButton) so clicking them steps by Increment — proving
// the custom ButtonSpinner theme replaced Fluent's wide side-by-side pair without losing function.
public class NumericSpinnerTests
{
    private static NumericUpDown Show()
    {
        var field = new NumericUpDown { Minimum = 0, Maximum = 100, Increment = 1, Value = 5 };
        var window = new Window { Width = 200, Height = 100, Content = field };
        window.Show();
        window.CaptureRenderedFrame();
        return field;
    }

    private static RepeatButton SpinButton(NumericUpDown field, string name)
        => field.GetVisualDescendants().OfType<RepeatButton>().First(b => b.Name == name);

    [AvaloniaFact]
    public void IncreaseButton_steps_up_by_increment()
    {
        var field = Show();
        SpinButton(field, "PART_IncreaseButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal(6m, field.Value);
    }

    [AvaloniaFact]
    public void DecreaseButton_steps_down_by_increment()
    {
        var field = Show();
        SpinButton(field, "PART_DecreaseButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal(4m, field.Value);
    }
}
