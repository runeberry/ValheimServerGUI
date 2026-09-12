using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using ValheimServerGUI.App.Views.Dialogs;
using Xunit;

namespace ValheimServerGUI.App.Tests.Services;

public class ConfirmWindowTests
{
    [AvaloniaFact]
    public void Yes_button_sets_result_true_and_closes()
    {
        var window = new ConfirmWindow("Confirm", "Are you sure?");
        window.Show();

        var yes = Assert.IsType<Button>(window.FindControl<Button>("YesButton"));
        yes.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.True(window.Result);
        Assert.False(window.IsVisible);
    }

    [AvaloniaFact]
    public void No_button_sets_result_false_and_closes()
    {
        var window = new ConfirmWindow("Confirm", "Are you sure?");
        window.Show();

        var no = Assert.IsType<Button>(window.FindControl<Button>("NoButton"));
        no.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.False(window.Result);
        Assert.False(window.IsVisible);
    }
}
