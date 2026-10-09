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
