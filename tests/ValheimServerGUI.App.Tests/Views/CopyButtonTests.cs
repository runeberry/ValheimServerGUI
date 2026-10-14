using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using ValheimServerGUI.App.Controls;
using Xunit;

namespace ValheimServerGUI.App.Tests.Views;

// The copy button flashes a checkmark on click and goes inert (but not disabled) while it shows.
public class CopyButtonTests
{
    [AvaloniaFact]
    public async Task Click_copies_shows_check_and_stays_colored_but_inert()
    {
        var button = new CopyButton { Text = "abcd-1234" };
        var window = new Window { Content = button };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var beforeIcon = button.CurrentIconSource;
        await button.ConfirmCopyAsync();

        Assert.NotEqual(beforeIcon, button.CurrentIconSource); // swapped to the checkmark
        Assert.True(button.IsConfirming);
        Assert.False(button.IsHitTestVisible); // inert to clicks
        Assert.True(button.IsEnabled);         // but NOT disabled — keeps its colour
    }

    [AvaloniaFact]
    public async Task Second_click_while_confirming_is_ignored()
    {
        var button = new CopyButton { Text = "x" };
        var window = new Window { Content = button };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        await button.ConfirmCopyAsync();
        var iconWhileConfirming = button.CurrentIconSource;
        await button.ConfirmCopyAsync(); // no-op while confirming

        Assert.True(button.IsConfirming);
        Assert.Equal(iconWhileConfirming, button.CurrentIconSource);
    }
}
