using ValheimServerGUI.App.Controls;
using Xunit;

namespace ValheimServerGUI.App.Tests.Views;

// HelpLabel is the single "?" glyph: it collapses when there is no help text and shows when there is.
public class HelpLabelTests
{
    [Fact]
    public void Hidden_when_help_text_is_empty()
    {
        var label = new HelpLabel();
        Assert.False(label.IsVisible);

        label.HelpText = "   ";
        Assert.False(label.IsVisible);
    }

    [Fact]
    public void Visible_when_help_text_is_set_and_collapses_again_when_cleared()
    {
        var label = new HelpLabel { HelpText = "What this field does." };
        Assert.True(label.IsVisible);

        label.HelpText = null;
        Assert.False(label.IsVisible);
    }
}
