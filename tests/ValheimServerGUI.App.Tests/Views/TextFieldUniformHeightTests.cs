using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ValheimServerGUI.App.Controls;
using Xunit;

namespace ValheimServerGUI.App.Tests.Views;

// Regression: a text box paired with a taller sibling (browse/copy button, show toggle) used to stretch
// vertically because it defaulted to VerticalAlignment=Stretch — giving those fields visibly more padding
// than a plain field. The field controls now center their box, so every text input is the same height.
public class TextFieldUniformHeightTests
{
    private static TextBox Realize(Control field)
    {
        var window = new Window { Width = 400, Height = 200, Content = field };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.Measure(new Size(400, 200));
        window.Arrange(new Rect(new Size(400, 200)));
        return field.GetVisualDescendants().OfType<TextBox>().First();
    }

    [AvaloniaFact]
    public void Text_boxes_are_uniform_height_regardless_of_trailing_content()
    {
        var plain = Realize(new TextFormField { LabelText = "Name", Value = "x" });
        var withTrailing = Realize(new TextFormField
        {
            LabelText = "Password", Value = "x",
            TrailingContent = new CheckBoxFormField { LabelText = "Show" },
        });
        var filename = Realize(new FilenameFormField { LabelText = "Exe", Value = "x" });

        Assert.Equal(plain.Bounds.Height, withTrailing.Bounds.Height);
        Assert.Equal(plain.Bounds.Height, filename.Bounds.Height);
        Assert.Equal(plain.Padding, filename.Padding);
    }
}
