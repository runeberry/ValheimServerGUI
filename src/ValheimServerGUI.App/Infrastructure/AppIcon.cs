using System;
using Avalonia.Controls;
using Avalonia.Platform;

namespace ValheimServerGUI.App.Infrastructure;

/// <summary>The application icon (window titlebar + system tray), loaded once from the embedded logo.</summary>
internal static class AppIcon
{
    private static WindowIcon? _cached;

    public static WindowIcon Load()
        => _cached ??= new WindowIcon(
            AssetLoader.Open(new Uri("avares://ValheimServerGUI.App/Assets/vsg_logo_256.png")));
}
