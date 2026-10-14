using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using ValheimServerGUI.App.Controls;
using ValheimServerGUI.Game;

namespace ValheimServerGUI.App.Converters;

/// <summary>
/// Maps a <see cref="PlayerStatus"/> to its 16×16 glyph shown beside the status text in the Players grid:
/// green online dot when online, the unsynced-arrows transition glyph while joining or leaving, and the
/// grey not-started glyph when offline.
/// </summary>
public sealed class PlayerStatusToIconConverter : IValueConverter
{
    public static readonly PlayerStatusToIconConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is PlayerStatus status ? AppIcons.Get(IconNameForStatus(status)) : null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    public static string IconNameForStatus(PlayerStatus status) => status switch
    {
        PlayerStatus.Online => "StatusOnline_16x",
        PlayerStatus.Joining or PlayerStatus.Leaving => "UnsyncedCommits_16x_Horiz",
        _ => "StatusNotStarted_16x",
    };
}
