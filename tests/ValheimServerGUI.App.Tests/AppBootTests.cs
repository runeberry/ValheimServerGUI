using Avalonia.Headless.XUnit;
using ValheimServerGUI.App.Views;
using Xunit;

namespace ValheimServerGUI.App.Tests;

public class AppBootTests
{
    [AvaloniaFact]
    public void MainWindow_shows()
    {
        var window = new MainWindow();
        window.Show();

        Assert.True(window.IsVisible);
    }
}
