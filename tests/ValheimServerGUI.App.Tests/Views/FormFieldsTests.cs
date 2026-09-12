using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ValheimServerGUI.App.Controls;
using Xunit;

namespace ValheimServerGUI.App.Tests.Views;

// The custom data-bound field controls wire their inner input to the field's Value via the template's
// #Root self-bindings. These realize each field in a window and assert the inner control reflects the
// field's Value — i.e. the wrapper's binding surface actually works.
public class FormFieldsTests
{
    private static T Realize<T>(T field) where T : Control
    {
        var window = new Window { Width = 300, Height = 200, Content = field };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.Measure(new Size(300, 200));
        window.Arrange(new Rect(new Size(300, 200)));
        return field;
    }

    private static TChild Child<TChild>(Visual root) where TChild : Control
        => root.GetVisualDescendants().OfType<TChild>().First();

    [AvaloniaFact]
    public void TextFormField_reflects_value()
    {
        var field = Realize(new TextFormField { LabelText = "Name", Value = "hello" });
        Assert.Equal("hello", Child<TextBox>(field).Text);
    }

    [AvaloniaFact]
    public void NumericFormField_reflects_value()
    {
        var field = Realize(new NumericFormField { LabelText = "Port", Minimum = 0, Maximum = 100, Value = 42 });
        Assert.Equal(42m, Child<NumericUpDown>(field).Value);
    }

    [AvaloniaFact]
    public void CheckBoxFormField_reflects_value_and_label()
    {
        var field = Realize(new CheckBoxFormField { LabelText = "Crossplay", Value = true });
        var box = Child<CheckBox>(field);
        Assert.True(box.IsChecked);
        Assert.Equal("Crossplay", box.Content);
    }

    [AvaloniaFact]
    public void DropdownFormField_reflects_value()
    {
        var field = Realize(new DropdownFormField
        {
            LabelText = "World",
            ItemsSource = new List<string> { "Alpha", "Beta" },
            Value = "Beta",
        });
        Assert.Equal("Beta", Child<ComboBox>(field).SelectedItem);
    }

    [AvaloniaFact]
    public void FilenameFormField_reflects_value()
    {
        var field = Realize(new FilenameFormField { LabelText = "Save Folder", Value = "/tmp/worlds" });
        Assert.Equal("/tmp/worlds", Child<TextBox>(field).Text);
    }
}
