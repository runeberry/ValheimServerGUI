namespace ValheimServerGUI.App;

/// <summary>Shell-level constants (product identity used by the OS-integration seams).</summary>
internal static class AppConstants
{
    /// <summary>Product name used for the window title, autostart entries, and the tray tooltip.</summary>
    public const string ProductName = "Valheim Server GUI";

    /// <summary>Stable identifier used as the autostart registration key / <c>.desktop</c> file name.</summary>
    public const string StartupKey = "ValheimServerGUI";

    // External links (parity with the v2.4 Resources URLs).
    public const string UrlHelp = "https://github.com/runeberry/ValheimServerGUI/wiki";
    public const string UrlPortForwarding = "https://github.com/runeberry/ValheimServerGUI/wiki/Connecting-to-your-Server";
    public const string UrlDiscord = "https://discord.gg/HBsNJTY";
    public const string UrlReleases = "https://github.com/runeberry/ValheimServerGUI/releases";
    public const string UrlDonate = "https://www.buymeacoffee.com/runeberry";
}
