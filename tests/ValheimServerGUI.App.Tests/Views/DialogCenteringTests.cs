using System;
using System.Linq;
using Avalonia.Controls;
using ValheimServerGUI.App.Views.Dialogs;
using Xunit;

namespace ValheimServerGUI.App.Tests.Views;

// The centralized owner-centering fix lives on DialogWindow. Guard the contract that every modal dialog
// inherits it, so a newly-added dialog can't silently fall back to Avalonia's mis-centering CenterOwner.
public class DialogCenteringTests
{
    [Fact]
    public void Every_dialog_window_derives_from_DialogWindow()
    {
        var offenders = typeof(MessageWindow).Assembly.GetTypes()
            .Where(t => t.Namespace == "ValheimServerGUI.App.Views.Dialogs")
            .Where(t => !t.IsAbstract && typeof(Window).IsAssignableFrom(t))
            .Where(t => t != typeof(DialogWindow))
            .Where(t => !typeof(DialogWindow).IsAssignableFrom(t))
            .Select(t => t.Name)
            .ToList();

        Assert.True(offenders.Count == 0,
            "These dialog windows must inherit DialogWindow for centralized owner-centering: " +
            string.Join(", ", offenders));
    }
}
