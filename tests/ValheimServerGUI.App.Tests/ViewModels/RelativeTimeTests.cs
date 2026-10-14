using System;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.Localization;
using Xunit;

namespace ValheimServerGUI.App.Tests.ViewModels;

public class RelativeTimeTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(0, nameof(Strings.RelativeTime_JustNow), null)]
    [InlineData(5, nameof(Strings.RelativeTime_JustNow), null)]
    [InlineData(30, nameof(Strings.RelativeTime_Seconds_Other), 30)]
    [InlineData(60, nameof(Strings.RelativeTime_Minutes_One), null)]
    [InlineData(120, nameof(Strings.RelativeTime_Minutes_Other), 2)]
    [InlineData(3600, nameof(Strings.RelativeTime_Hours_One), null)]
    [InlineData(7200, nameof(Strings.RelativeTime_Hours_Other), 2)]
    [InlineData(86400, nameof(Strings.RelativeTime_Days_One), null)]
    [InlineData(172800, nameof(Strings.RelativeTime_Days_Other), 2)]
    public void Past(int secondsAgo, string unitKey, int? count)
    {
        var when = Now - TimeSpan.FromSeconds(secondsAgo);
        var unit = string.Format(Strings.ResourceManager.GetString(unitKey)!, count);
        var expected = unitKey == nameof(Strings.RelativeTime_JustNow) ? unit : string.Format(Strings.RelativeTime_Past, unit);
        Assert.Equal(expected, RelativeTime.Format(when, Now));
    }

    [Fact]
    public void Future_uses_in_prefix()
    {
        var when = Now + TimeSpan.FromMinutes(5);
        Assert.Equal(string.Format(Strings.RelativeTime_Future, string.Format(Strings.RelativeTime_Minutes_Other, 5)),
            RelativeTime.Format(when, Now));
    }
}
