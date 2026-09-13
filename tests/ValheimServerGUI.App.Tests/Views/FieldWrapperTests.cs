using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ValheimServerGUI.App.Controls;
using Xunit;

namespace ValheimServerGUI.App.Tests.Views;

// The Radio/Label field wrappers bind their Value through to the inner control they wrap.
public class FieldWrapperTests
{
    [AvaloniaFact]
    public void RadioFormField_value_tracks_inner_ischecked_both_ways()
    {
        var field = new RadioFormField { LabelText = "New", GroupName = "World" };
        var window = new Window { Content = field };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var radio = field.GetVisualDescendants().OfType<RadioButton>().First();

        field.Value = true;
        Dispatcher.UIThread.RunJobs();
        Assert.True(radio.IsChecked);

        radio.IsChecked = false;
        Dispatcher.UIThread.RunJobs();
        Assert.False(field.Value);
    }

    [AvaloniaFact]
    public void LabelField_reflects_value_updates()
    {
        var field = new LabelField { LabelText = "External IP:", Value = "1.2.3.4" };
        var window = new Window { Content = field };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var valueLabel = field.GetVisualDescendants().OfType<SelectableTextBlock>().First();
        Assert.Equal("1.2.3.4", valueLabel.Text);

        field.Value = "5.6.7.8";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("5.6.7.8", valueLabel.Text);
    }
}
