using System;
using System.Globalization;
using ValheimServerGUI.App.Converters;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools.Models;
using Xunit;

namespace ValheimServerGUI.App.Tests.Converters;

public class ConverterTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(0, "just now")]
    [InlineData(5, "just now")]
    [InlineData(30, "30 seconds ago")]
    [InlineData(60, "1 minute ago")]
    [InlineData(120, "2 minutes ago")]
    [InlineData(3600, "1 hour ago")]
    [InlineData(7200, "2 hours ago")]
    [InlineData(86400, "1 day ago")]
    [InlineData(172800, "2 days ago")]
    public void RelativeTime_past(int secondsAgo, string expected)
    {
        var when = Now - TimeSpan.FromSeconds(secondsAgo);
        Assert.Equal(expected, RelativeTimeConverter.Format(when, Now));
    }

    [Fact]
    public void RelativeTime_future_uses_in_prefix()
    {
        var when = Now + TimeSpan.FromMinutes(5);
        Assert.Equal("in 5 minutes", RelativeTimeConverter.Format(when, Now));
    }

    [Fact]
    public void RelativeTime_converter_uses_now_provider()
    {
        var conv = new RelativeTimeConverter { NowProvider = () => Now };
        var result = conv.Convert(Now - TimeSpan.FromMinutes(3), typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("3 minutes ago", result);
    }

    [Theory]
    [InlineData(ServerStatus.Stopped)]
    [InlineData(ServerStatus.Starting)]
    [InlineData(ServerStatus.Running)]
    [InlineData(ServerStatus.Stopping)]
    public void StatusToBrush_maps_every_status(ServerStatus status)
    {
        var brush = StatusToBrushConverter.ForStatus(status);
        Assert.NotNull(brush);

        if (status == ServerStatus.Running)
            Assert.Same(StatusToBrushConverter.Running, brush);
        else if (status is ServerStatus.Starting or ServerStatus.Stopping)
            Assert.Same(StatusToBrushConverter.Transitioning, brush);
        else
            Assert.Same(StatusToBrushConverter.Stopped, brush);
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
