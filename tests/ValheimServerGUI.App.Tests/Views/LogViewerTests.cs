using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ValheimServerGUI.App.Controls;
using Xunit;

namespace ValheimServerGUI.App.Tests.Views;

// LogViewer renders its bound lines as one contiguous, selectable block (so logs copy-paste in bulk) and
// appends as new lines arrive.
public class LogViewerTests
{
    private static (LogViewer, SelectableTextBlock) Realize(ObservableCollection<string> lines)
    {
        var viewer = new LogViewer { ItemsSource = lines };
        var window = new Window { Width = 300, Height = 200, Content = viewer };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var text = viewer.GetVisualDescendants().OfType<SelectableTextBlock>().First();
        return (viewer, text);
    }

    [AvaloniaFact]
    public void Lines_render_as_one_newline_joined_block()
    {
        var (_, text) = Realize(new ObservableCollection<string> { "alpha", "beta", "gamma" });
        Assert.Equal("alpha\nbeta\ngamma", text.Text);
    }

    [AvaloniaFact]
    public void New_lines_are_appended()
    {
        var lines = new ObservableCollection<string> { "first" };
        var (_, text) = Realize(lines);

        lines.Add("second");
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("first\nsecond", text.Text);
    }
}
