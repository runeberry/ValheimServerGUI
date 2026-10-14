using Avalonia.Headless.XUnit;
using ValheimServerGUI.App.Controls;
using Xunit;

namespace ValheimServerGUI.App.Tests.Views;

// DurationFormField keeps Value in canonical seconds but shows it as amount + unit. These pin the
// seconds <-> (amount, unit) conversion both ways, plus that the per-unit spinner bounds honour the
// seconds bounds.
public class DurationFormFieldTests
{
    [AvaloniaTheory]
    [InlineData(1800, DurationUnit.Minutes, 30)]   // 30 minutes
    [InlineData(7200, DurationUnit.Hours, 2)]      // 2 hours
    [InlineData(43200, DurationUnit.Hours, 12)]    // 12 hours (not evenly days)
    [InlineData(86400, DurationUnit.Days, 1)]      // 1 day
    [InlineData(90, DurationUnit.Seconds, 90)]     // no clean larger unit
    [InlineData(0, DurationUnit.Seconds, 0)]
    public void Value_is_shown_in_the_cleanest_unit(int seconds, DurationUnit unit, int amount)
    {
        var field = new DurationFormField { Minimum = 0, Maximum = 2592000, Value = seconds };

        Assert.Equal(unit, field.SelectedUnit);
        Assert.Equal(amount, field.DisplayAmount);
    }

    [AvaloniaFact]
    public void Editing_amount_and_unit_writes_back_seconds()
    {
        var field = new DurationFormField { Minimum = 60, Maximum = 86400, Value = 1800 };

        field.SelectedUnit = DurationUnit.Hours;
        field.DisplayAmount = 3;

        Assert.Equal(3 * 3600, field.Value);
    }

    [AvaloniaFact]
    public void Spinner_bounds_are_derived_per_unit_from_the_seconds_bounds()
    {
        // 60s..86400s expressed in hours: ceil(60/3600)=1 .. floor(86400/3600)=24.
        var field = new DurationFormField { Minimum = 60, Maximum = 86400, Value = 3600 };

        field.SelectedUnit = DurationUnit.Hours;

        Assert.Equal(1, field.NumericMinimum);
        Assert.Equal(24, field.NumericMaximum);
    }

    [AvaloniaFact]
    public void Amount_is_clamped_into_the_units_range()
    {
        // Switching 30 minutes to hours, where the max is 24h, clamps the shown amount (and the seconds).
        var field = new DurationFormField { Minimum = 60, Maximum = 86400, Value = 1800 };

        field.SelectedUnit = DurationUnit.Hours;

        Assert.Equal(24, field.DisplayAmount);
        Assert.Equal(86400, field.Value);
    }
}
