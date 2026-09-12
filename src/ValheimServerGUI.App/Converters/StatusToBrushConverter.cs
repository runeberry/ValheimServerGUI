using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ValheimServerGUI.Game;

namespace ValheimServerGUI.App.Converters;

/// <summary>
/// Maps a <see cref="ServerStatus"/> to the status-indicator colour shown in the status bar and tray
/// (grey = stopped, amber = transitioning, green = running).
/// </summary>
public sealed class StatusToBrushConverter : IValueConverter
{
    public static readonly StatusToBrushConverter Instance = new();

    public static readonly IBrush Stopped = new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E));
    public static readonly IBrush Transitioning = new SolidColorBrush(Color.FromRgb(0xF5, 0xA6, 0x23));
    public static readonly IBrush Running = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is ServerStatus status ? ForStatus(status) : Stopped;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    public static IBrush ForStatus(ServerStatus status) => status switch
    {
        ServerStatus.Running => Running,
        ServerStatus.Starting or ServerStatus.Stopping => Transitioning,
        _ => Stopped,
    };
}
