using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ValheimServerGUI.App.Views.Dialogs;
using Xunit;

namespace ValheimServerGUI.App.Tests.Views;

// The single reusable message-box modal: buttons are built from the specs, clicking one closes the window
// with that button's result, and the default/cancel roles reach the actual Button controls.
public class MessageBoxWindowTests
{
    private static Button ButtonNamed(MessageBoxWindow window, string caption)
        => window.GetVisualDescendants().OfType<Button>().First(b => (string?)b.Content == caption);

    [AvaloniaFact]
    public void Clicking_a_button_records_its_result_and_closes()
    {
        var window = new MessageBoxWindow("Confirm", "Are you sure?", new[]
        {
            new MessageBoxButton("Yes", true, isDefault: true),
            new MessageBoxButton("No", false, isCancel: true),
        });
        window.Show();

        ButtonNamed(window, "No").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.Equal(false, window.Result);
        Assert.False(window.IsVisible);
    }

    [AvaloniaFact]
    public void Enum_results_round_trip_through_the_clicked_button()
    {
        var window = new MessageBoxWindow("Player Role Conflicts", "body", new[]
        {
            new MessageBoxButton("Use server profile", RoleConflictChoice.UseServerProfile, isDefault: true),
            new MessageBoxButton("Use roles from file", RoleConflictChoice.UseRolesFromFile),
            new MessageBoxButton("Cancel", RoleConflictChoice.Cancel, isCancel: true),
        });
        window.Show();

        ButtonNamed(window, "Use roles from file").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.Equal(RoleConflictChoice.UseRolesFromFile, window.Result);
    }

    [AvaloniaFact]
    public void Default_and_cancel_roles_reach_the_buttons()
    {
        var window = new MessageBoxWindow("T", "m", new[]
        {
            new MessageBoxButton("A", 1, isDefault: true),
            new MessageBoxButton("B", 2, isCancel: true),
        });
        window.Show();

        Assert.True(ButtonNamed(window, "A").IsDefault);
        Assert.True(ButtonNamed(window, "B").IsCancel);
    }
}
