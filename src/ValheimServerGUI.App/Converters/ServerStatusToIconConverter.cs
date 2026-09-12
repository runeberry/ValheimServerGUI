using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using ValheimServerGUI.App.Controls;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.Game;

namespace ValheimServerGUI.App.Converters;

/// <summary>
/// Maps a <see cref="ServerStatus"/> to its 16×16 status-bar icon, mirroring the WinForms
/// <c>ServerStatusIconMap</c> (grey pause = stopped, unsynced arrows = transitioning, green = running).
/// </summary>
public sealed class ServerStatusToIconConverter : IValueConverter
{
    public static readonly ServerStatusToIconConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is ServerStatus status ? AppIcons.Get(IconNameForStatus(status)) : null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    public static string IconNameForStatus(ServerStatus status) => status switch
    {
        ServerStatus.Running => "StatusRun_16x",
        ServerStatus.Starting or ServerStatus.Stopping => "UnsyncedCommits_16x_Horiz",
        _ => "StatusPause_grey_16x",
    };
}

/// <summary>
/// Maps the <see cref="UpdateCheckStatus"/> outcome to its 16×16 status-bar icon (spinner while checking,
/// green check when up to date, amber warning when an update is available, red error on failure). No icon
/// before the first check.
/// </summary>
public sealed class UpdateStatusToIconConverter : IValueConverter
{
    public static readonly UpdateStatusToIconConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not UpdateCheckStatus status) return null;
        var name = IconNameForStatus(status);
        return name is null ? null : AppIcons.Get(name);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    public static string? IconNameForStatus(UpdateCheckStatus status) => status switch
    {
        UpdateCheckStatus.Checking => "Loading_Blue_16x",
        UpdateCheckStatus.UpToDate => "StatusOK_16x",
        UpdateCheckStatus.Available => "StatusWarning_16x",
        UpdateCheckStatus.Error => "StatusCriticalError_16x",
        _ => null,
    };
}
