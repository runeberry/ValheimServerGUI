using System;
using System.Globalization;
using System.Linq;
using ValheimServerGUI.App.ViewModels.Dialogs;
using ValheimServerGUI.Game;
using Xunit;

namespace ValheimServerGUI.App.Tests.ViewModels.Dialogs;

// WorldGenDisplay maps display text back to tokens by string match, so every culture's text must keep the
// displays within one table distinct. Checked in English and in the key-echo test culture.
public class WorldGenDisplayTests
{
    [Theory]
    [InlineData("en")]
    [InlineData("qps-ploc")]
    public void Every_modifier_value_round_trips_through_its_display(string culture)
    {
        WithUiCulture(culture, () =>
        {
            foreach (var key in WorldGenModifiers.All)
            {
                var displays = WorldGenDisplay.ModifierValueDisplays(key);
                Assert.Equal(displays.Count, displays.Distinct().Count());

                foreach (var display in displays)
                {
                    var token = WorldGenDisplay.ModifierValueToken(key, display);
                    Assert.Equal(display, WorldGenDisplay.ModifierValueDisplay(key, token));
                }

                Assert.Null(WorldGenDisplay.ModifierValueToken(key, WorldGenDisplay.NormalModifier));
            }
        });
    }

    [Theory]
    [InlineData("en")]
    [InlineData("qps-ploc")]
    public void Every_preset_round_trips_through_its_display(string culture)
    {
        WithUiCulture(culture, () =>
        {
            var displays = WorldGenDisplay.PresetDisplays;
            Assert.Equal(displays.Count, displays.Distinct().Count());

            foreach (var display in displays)
                Assert.Equal(display, WorldGenDisplay.PresetDisplay(WorldGenDisplay.PresetToken(display)));

            Assert.Equal(WorldPreferencesViewModel.CustomPreset, WorldGenDisplay.PresetToken(displays[0]));
        });
    }

    private static void WithUiCulture(string name, Action body)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);
            body();
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
