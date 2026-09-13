using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using ValheimServerGUI.App.Controls;
using Xunit;

namespace ValheimServerGUI.App.Tests.Views;

// HyperlinkLabel is a real text label (not a button) that invokes its Command when clicked.
public class HyperlinkLabelTests
{
    [AvaloniaFact]
    public void Click_invokes_command_with_parameter()
    {
        object? received = null;
        var link = new HyperlinkLabel
        {
            Text = "Click here.",
            CommandParameter = "payload",
            Command = new RelayCommand<object?>(p => received = p),
        };

        var window = new Window { Content = link, Width = 200, Height = 60 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.Measure(new Size(200, 60));
        window.Arrange(new Rect(new Size(200, 60)));
        Dispatcher.UIThread.RunJobs();

        var center = link.TranslatePoint(new Point(link.Bounds.Width / 2, link.Bounds.Height / 2), window)!.Value;
        window.MouseDown(center, MouseButton.Left);
        window.MouseUp(center, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("payload", received);
    }
}
