using Avalonia;
using ValheimServerGUI.App.Views.Dialogs;
using Xunit;

namespace ValheimServerGUI.App.Tests.Views;

// DialogWindow centres each dialog over its owner, then keeps it inside the owner screen's working area.
// Geometry below is in physical pixels; the 1920x1152 area is a 1920x1200 screen minus a 48px taskbar.
public class DialogPlacementTests
{
    private static readonly PixelRect WorkingArea = new(0, 0, 1920, 1152);
    private static readonly PixelSize ManagePlayers = new(562, 540);

    [Fact]
    public void Dialog_that_fits_is_centred_over_its_owner()
    {
        var owner = new PixelRect(687, 360, 552, 440);

        var position = DialogWindow.PlaceOverOwner(owner, ManagePlayers, WorkingArea);

        Assert.Equal(new PixelPoint(682, 310), position);
    }

    [Fact]
    public void Dialog_taller_than_an_owner_near_the_top_stays_on_screen()
    {
        // Owner at the top-left of the screen: plain centring would place the dialog at y = -24.
        var owner = new PixelRect(33, 26, 552, 440);

        var position = DialogWindow.PlaceOverOwner(owner, ManagePlayers, WorkingArea);

        Assert.Equal(new PixelPoint(28, 0), position);
    }

    [Fact]
    public void Dialog_near_the_bottom_right_is_kept_above_the_taskbar()
    {
        var owner = new PixelRect(1500, 900, 552, 440);

        var position = DialogWindow.PlaceOverOwner(owner, ManagePlayers, WorkingArea);

        Assert.Equal(new PixelPoint(1920 - 562, 1152 - 540), position);
    }

    [Fact]
    public void Dialog_larger_than_the_working_area_is_pinned_to_its_top_left()
    {
        var owner = new PixelRect(100, 100, 552, 440);
        var huge = new PixelSize(2400, 1400);

        var position = DialogWindow.PlaceOverOwner(owner, huge, WorkingArea);

        Assert.Equal(new PixelPoint(0, 0), position);
    }

    [Fact]
    public void Working_area_on_a_secondary_monitor_is_respected()
    {
        // A monitor to the left of the primary has negative coordinates.
        var leftMonitor = new PixelRect(-1920, 0, 1920, 1040);
        var owner = new PixelRect(-1900, 10, 552, 440);

        var position = DialogWindow.PlaceOverOwner(owner, ManagePlayers, leftMonitor);

        Assert.Equal(new PixelPoint(-1905, 0), position);
    }

    [Fact]
    public void Without_a_known_working_area_the_dialog_is_only_centred()
    {
        var owner = new PixelRect(33, 26, 552, 440);

        var position = DialogWindow.PlaceOverOwner(owner, ManagePlayers, workingArea: null);

        Assert.Equal(new PixelPoint(28, -24), position);
    }
}
