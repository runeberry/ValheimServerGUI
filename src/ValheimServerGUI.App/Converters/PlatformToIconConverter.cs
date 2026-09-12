using System;
using System.Globalization;
using Avalonia.Data.Converters;
using ValheimServerGUI.Tools.Models;

namespace ValheimServerGUI.App.Converters;

/// <summary>
/// Maps a player-platform string (<see cref="PlayerPlatforms"/>) to a short glyph shown in the players
/// grid. A placeholder text glyph for now; a Wave 5 icon asset can drop in without changing bindings.
/// </summary>
public sealed class PlatformToIconConverter : IValueConverter
{
    public static readonly PlatformToIconConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => ForPlatform(value as string);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    public static string ForPlatform(string? platform)
    {
        if (PlayerPlatforms.TryGetValidPlatform(platform, out var valid))
        {
            return valid switch
            {
                PlayerPlatforms.Steam => "🅢",
                PlayerPlatforms.Xbox => "🄭",
                _ => "•",
            };
        }

        return "•";
    }
}
