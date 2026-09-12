using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ValheimServerGUI.App.Controls;
using Xunit;

namespace ValheimServerGUI.App.Tests.Views;

// Regression: the compact spinner binds its inner box padding to NumericUpDown.Padding (default 0), so a
// number field used to press the digits against the left edge. It must match the text-field padding.
public class NumericFieldPaddingTests
{
    [AvaloniaFact]
    public void Numeric_inner_padding_matches_text_field()
    {
        var num = new NumericFormField { LabelText = "Port", Minimum = 1, Maximum = 100, Value = 42 };
        var text = new TextFormField { LabelText = "Name", Value = "x" };
        var window = new Window { Width = 300, Height = 200, Content = new StackPanel { Children = { num, text } } };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.Measure(new Size(300, 200));
        window.Arrange(new Rect(new Size(300, 200)));

        var numBox = num.GetVisualDescendants().OfType<TextBox>().First();
        var textBox = text.GetVisualDescendants().OfType<TextBox>().First();
        Assert.Equal(textBox.Padding, numBox.Padding);
        Assert.True(numBox.Padding.Left > 0);
    }
}
