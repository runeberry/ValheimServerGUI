using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using ValheimServerGUI.App.Tests.Fakes;
using ValheimServerGUI.App.ViewModels.Dialogs;
using ValheimServerGUI.App.Views.Dialogs;
using Xunit;

namespace ValheimServerGUI.App.Tests.Views;

// The non-localized AutomationIds the reboot-test UI drivers look controls up by. They must land on the
// element automation acts on (the inner CheckBox, the Button), not only on a wrapping control.
public class AutomationIdTests
{
    private static T WithId<T>(Window window, string id) where T : Control
        => window.GetVisualDescendants().OfType<T>().Single(c => AutomationProperties.GetAutomationId(c) == id);

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Start_with_os_checkbox_carries_its_id_and_the_registration_state(bool enabled)
    {
        var window = new PreferencesWindow(new PreferencesViewModel(
            new FakeUserPreferencesProvider(), new FakeStartupManager { Enabled = enabled }));
        window.Show();

        Assert.Equal(enabled, WithId<CheckBox>(window, "Preferences_StartWithOs").IsChecked);
        Assert.True(WithId<Button>(window, "Preferences_OK").IsDefault);
    }

    [AvaloniaFact]
    public void Message_box_buttons_are_identified_by_their_result()
    {
        var window = new MessageBoxWindow("T", "m", new[]
        {
            new MessageBoxButton("Yes", true, isDefault: true),
            new MessageBoxButton("No", false, isCancel: true),
            new MessageBoxButton("OK"),
        });
        window.Show();

        Assert.Equal("Yes", WithId<Button>(window, "MessageBox_True").Content);
        Assert.Equal("No", WithId<Button>(window, "MessageBox_False").Content);
        Assert.Equal("OK", WithId<Button>(window, "MessageBox_OK").Content);
    }
}
