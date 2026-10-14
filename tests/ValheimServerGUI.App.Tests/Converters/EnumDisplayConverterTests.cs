using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ValheimServerGUI.App.Controls;
using ValheimServerGUI.App.Converters;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.Game;
using Xunit;

namespace ValheimServerGUI.App.Tests.Converters;

public class EnumDisplayConverterTests
{
    // Every value of every enum the UI displays. A value missing from its switch falls back to ToString(),
    // which under the key-echo test culture is the only way to produce text without the ⟦…⟧ brackets.
    public static IEnumerable<object[]> DisplayedEnumValues()
        => new[] { typeof(ServerStatus), typeof(PlayerStatus), typeof(PlayerRole), typeof(AppTheme), typeof(DurationUnit), typeof(LogView) }
            .SelectMany(t => Enum.GetValues(t).Cast<object>())
            .Select(v => new[] { v });

    [Theory]
    [MemberData(nameof(DisplayedEnumValues))]
    public void Every_displayed_enum_value_has_localized_text(object value)
    {
        var text = EnumDisplayConverter.Instance.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);

        var s = Assert.IsType<string>(text);
        Assert.StartsWith("⟦", s);
    }

    [Fact]
    public void Non_enum_items_display_as_themselves()
    {
        Assert.Equal("My Profile", EnumDisplayConverter.Instance.Convert("My Profile", typeof(string), null, CultureInfo.InvariantCulture));
    }
}
