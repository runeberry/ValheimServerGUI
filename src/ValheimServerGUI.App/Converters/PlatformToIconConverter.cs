using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using ValheimServerGUI.App.Controls;
using ValheimServerGUI.Tools.Models;

namespace ValheimServerGUI.App.Converters;

/// <summary>
/// Maps a player-platform string (<see cref="PlayerPlatforms"/>) to its 16×16 icon shown in the players
/// grid. Unknown/absent platforms map to no icon (null) rather than a placeholder glyph.
/// </summary>
public sealed class PlatformToIconConverter : IValueConverter
{
    public static readonly PlatformToIconConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => ForPlatform(value as string);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    /// <summary>The icon asset base name for a platform, or null if there is no icon for it.</summary>
    public static string? IconNameForPlatform(string? platform)
    {
        if (PlayerPlatforms.TryGetValidPlatform(platform, out var valid))
        {
            return valid switch
            {
                PlayerPlatforms.Steam => "Steam_16x",
                PlayerPlatforms.Xbox => "XboxLive_16x",
                _ => null,
            };
        }

        return null;
    }

    /// <summary>The platform icon bitmap, or null when the platform is unknown.</summary>
    public static Bitmap? ForPlatform(string? platform)
    {
        var name = IconNameForPlatform(platform);
        return name is null ? null : AppIcons.Get(name);
    }
}
