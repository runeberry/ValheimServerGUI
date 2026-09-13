using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using ValheimServerGUI.App.Controls;
using Xunit;

namespace ValheimServerGUI.App.Tests.Views;

// IconButton flashes its confirm glyph on click when ConfirmIconName is set, and goes inert (not disabled)
// while it shows; with no confirm icon a click leaves the glyph unchanged.
public class IconButtonTests
{
    private static void Click(Window window, IconButton button)
    {
        window.Content = button;
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.Measure(new Size(120, 60));
        window.Arrange(new Rect(new Size(120, 60)));
        Dispatcher.UIThread.RunJobs();

        var center = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;
        window.MouseDown(center, MouseButton.Left);
        window.MouseUp(center, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void Click_flashes_confirm_icon_and_stays_colored_but_inert()
    {
        var button = new IconButton { IconName = "Edit_16x", ConfirmIconName = "StatusOK_16x" };
        var before = button.CurrentIconSource;

        Click(new Window { Width = 120, Height = 60 }, button);

        Assert.True(button.IsConfirming);
        Assert.NotEqual(before, button.CurrentIconSource); // swapped to the confirm glyph
        Assert.False(button.IsHitTestVisible);             // inert to clicks
        Assert.True(button.IsEnabled);                     // but not disabled — keeps its colour
    }

    [AvaloniaFact]
    public void Click_without_confirm_icon_leaves_glyph_unchanged()
    {
        var button = new IconButton { IconName = "OpenFolder_16x" };
        var before = button.CurrentIconSource;

        Click(new Window { Width = 120, Height = 60 }, button);

        Assert.False(button.IsConfirming);
        Assert.Equal(before, button.CurrentIconSource);
    }
}
