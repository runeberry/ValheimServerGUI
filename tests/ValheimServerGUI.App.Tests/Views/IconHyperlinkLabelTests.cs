using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;
using ValheimServerGUI.App.Controls;
using Xunit;

namespace ValheimServerGUI.App.Tests.Views;

// IconHyperlinkLabel is an icon + HyperlinkLabel acting as ONE link: both parts click, and a click runs once.
public class IconHyperlinkLabelTests
{
    [AvaloniaTheory]
    [InlineData(typeof(IconImage))]
    [InlineData(typeof(HyperlinkLabel))]
    public void Clicking_icon_or_text_invokes_command_once(System.Type part)
    {
        var clicks = 0;
        var link = new IconHyperlinkLabel
        {
            IconName = "DiscordLogo",
            Text = "Discord",
            Command = new RelayCommand(() => clicks++),
        };
        var window = Show(link);

        var target = (Control)link.GetVisualDescendants().First(part.IsInstanceOfType);
        var center = target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), window)!.Value;
        window.MouseDown(center, MouseButton.Left);
        window.MouseUp(center, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void Shows_the_icon_and_text()
    {
        var link = new IconHyperlinkLabel { IconName = "DiscordLogo", Text = "Discord" };
        Show(link);

        Assert.NotNull(link.GetVisualDescendants().OfType<IconImage>().Single().Source);
        Assert.Equal("Discord", link.GetVisualDescendants().OfType<HyperlinkLabel>().Single().Text);
    }

    private static Window Show(Control content)
    {
        var window = new Window { Content = content, Width = 200, Height = 60 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.Measure(new Size(200, 60));
        window.Arrange(new Rect(new Size(200, 60)));
        Dispatcher.UIThread.RunJobs();
        return window;
    }
}
