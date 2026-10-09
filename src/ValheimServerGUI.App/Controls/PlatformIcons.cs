using Avalonia.Media.Imaging;
using ValheimServerGUI.Tools.Models;

namespace ValheimServerGUI.App.Controls;

/// <summary>
/// Maps a player-platform string (<see cref="PlayerPlatforms"/>) to its 16×16 icon shown in the player tables.
/// Unknown/absent platforms map to no icon (null) rather than a placeholder glyph.
/// </summary>
public static class PlatformIcons
{
    /// <summary>The icon asset base name for a platform, or null if there is no icon for it.</summary>
    public static string? IconNameForPlatform(string? platform)
    {
        if (PlayerPlatforms.TryGetValidPlatform(platform, out var valid))
        {
            return valid switch
            {
                PlayerPlatforms.Steam => "Steam_16x",
                PlayerPlatforms.Xbox => "XboxLive_16x",
                PlayerPlatforms.PlayStation => "PlayStation_16x",
                PlayerPlatforms.Nintendo => "Nintendo_16x",
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
