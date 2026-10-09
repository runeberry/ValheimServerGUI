using System;
using System.Globalization;
using ValheimServerGUI.App.Converters;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.Game;
using ValheimServerGUI.Localization;
using ValheimServerGUI.Tools.Models;
using Xunit;

namespace ValheimServerGUI.App.Tests.Converters;

public class ConverterTests
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
    public void RelativeTime_past(int secondsAgo, string unitKey, int? count)
    {
        var when = Now - TimeSpan.FromSeconds(secondsAgo);
        var unit = string.Format(Strings.ResourceManager.GetString(unitKey)!, count);
        var expected = unitKey == nameof(Strings.RelativeTime_JustNow) ? unit : string.Format(Strings.RelativeTime_Past, unit);
        Assert.Equal(expected, RelativeTimeConverter.Format(when, Now));
    }

    [Fact]
    public void RelativeTime_future_uses_in_prefix()
    {
        var when = Now + TimeSpan.FromMinutes(5);
        Assert.Equal(string.Format(Strings.RelativeTime_Future, string.Format(Strings.RelativeTime_Minutes_Other, 5)),
            RelativeTimeConverter.Format(when, Now));
    }

    [Fact]
    public void RelativeTime_converter_uses_now_provider()
    {
        var conv = new RelativeTimeConverter { NowProvider = () => Now };
        var result = conv.Convert(Now - TimeSpan.FromMinutes(3), typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal(string.Format(Strings.RelativeTime_Past, string.Format(Strings.RelativeTime_Minutes_Other, 3)), result);
    }

    [Theory]
    [InlineData("Steam", "Steam_16x")]
    [InlineData("steam", "Steam_16x")]
    [InlineData("Xbox", "XboxLive_16x")]
    public void PlatformToIcon_known_platforms_map_to_an_icon(string platform, string expected)
    {
        Assert.Equal(expected, PlatformToIconConverter.IconNameForPlatform(platform));
    }

    [Fact]
    public void PlatformToIcon_unknown_has_no_icon()
    {
        Assert.Null(PlatformToIconConverter.IconNameForPlatform("nope"));
        Assert.Null(PlatformToIconConverter.IconNameForPlatform(null));
    }

    [Theory]
    [InlineData(ServerStatus.Stopped, "StatusPause_grey_16x")]
    [InlineData(ServerStatus.Starting, "UnsyncedCommits_16x_Horiz")]
    [InlineData(ServerStatus.Running, "StatusRun_16x")]
    [InlineData(ServerStatus.Stopping, "UnsyncedCommits_16x_Horiz")]
    public void ServerStatusToIcon_maps_every_status(ServerStatus status, string expected)
    {
        Assert.Equal(expected, ServerStatusToIconConverter.IconNameForStatus(status));
    }

    [Theory]
    [InlineData(PlayerStatus.Online, "StatusOnline_16x")]
    [InlineData(PlayerStatus.Joining, "UnsyncedCommits_16x_Horiz")]
    [InlineData(PlayerStatus.Leaving, "UnsyncedCommits_16x_Horiz")]
    [InlineData(PlayerStatus.Offline, "StatusNotStarted_16x")]
    public void PlayerStatusToIcon_maps_every_status(PlayerStatus status, string expected)
    {
        Assert.Equal(expected, PlayerStatusToIconConverter.IconNameForStatus(status));
    }

    [Theory]
    [InlineData(UpdateCheckStatus.None, null)]
    [InlineData(UpdateCheckStatus.Checking, "Loading_Blue_16x")]
    [InlineData(UpdateCheckStatus.UpToDate, "StatusOK_16x")]
    [InlineData(UpdateCheckStatus.Available, "StatusWarning_16x")]
    [InlineData(UpdateCheckStatus.Error, "StatusCriticalError_16x")]
    public void UpdateStatusToIcon_maps_every_status(UpdateCheckStatus status, string? expected)
    {
        Assert.Equal(expected, UpdateStatusToIconConverter.IconNameForStatus(status));
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("x", true)]
    public void StringNotEmpty(string? value, bool expected)
    {
        Assert.Equal(expected, StringNotEmptyConverter.Instance.Convert(value, typeof(bool), null, CultureInfo.InvariantCulture));
    }
}
