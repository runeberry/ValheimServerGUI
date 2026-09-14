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
// vertically, giving those fields visibly more padding than a plain field. Every field's visible input box
// must be the same height — for a plain field that box is the TextBox; for a shell-wrapped field
// (FilenameFormField) it is the outlined Border.fieldShell that hosts the chromeless box + end-caps.
public class TextFieldUniformHeightTests
{
    private static void Realize(Control field)
    {
        var window = new Window { Width = 400, Height = 200, Content = field };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.Measure(new Size(400, 200));
        window.Arrange(new Rect(new Size(400, 200)));
    }

    private static TextBox Box(Control field) =>
        field.GetVisualDescendants().OfType<TextBox>().First();

    private static Border Shell(Control field) =>
        field.GetVisualDescendants().OfType<Border>().First(b => b.Classes.Contains("fieldShell"));

    [AvaloniaFact]
    public void Input_boxes_are_uniform_height_regardless_of_trailing_content()
    {
        var plainField = new TextFormField { LabelText = "Name", Value = "x" };
        var withTrailingField = new TextFormField
        {
            LabelText = "Password", Value = "x",
            TrailingContent = new CheckBoxFormField { LabelText = "Show" },
        };
        var filenameField = new FilenameFormField { LabelText = "Exe", Value = "x" };

        Realize(plainField);
        Realize(withTrailingField);
        Realize(filenameField);

        var plainHeight = Box(plainField).Bounds.Height;

        // Two bare boxes must match exactly (the original regression was a several-px stretch).
        Assert.Equal(plainHeight, Box(withTrailingField).Bounds.Height);
        // The shell (border + chromeless box) matches a bare box to within a pixel of border/metric rounding.
        Assert.True(System.Math.Abs(plainHeight - Shell(filenameField).Bounds.Height) <= 1,
            $"shell height {Shell(filenameField).Bounds.Height} should match plain box {plainHeight} within 1px");
        Assert.Equal(Box(plainField).Padding, Box(filenameField).Padding); // same inner text inset
    }
}
