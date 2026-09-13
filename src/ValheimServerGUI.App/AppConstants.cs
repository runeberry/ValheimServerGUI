namespace ValheimServerGUI.App;

/// <summary>Shell-level constants (product identity used by the OS-integration seams).</summary>
internal static class AppConstants
{
    /// <summary>Product name used for the window title, autostart entries, and the tray tooltip.</summary>
    public const string ProductName = "Valheim Server GUI";

    /// <summary>Stable identifier used as the autostart registration key / <c>.desktop</c> file name.</summary>
    public const string StartupKey = "ValheimServerGUI";

    /// <summary>
    /// View-only marker for a world that lives only in Steam Cloud. Applied and stripped exclusively by the
    /// world-select surface — it never reaches saved prefs, server options, or validation (§ locked decisions).
    /// </summary>
    public const string CloudWorldSuffix = " (cloud)";

    // External links (parity with the v2.4 Resources URLs).
    public const string UrlHelp = "https://github.com/runeberry/ValheimServerGUI/wiki";
    public const string UrlPortForwarding = "https://github.com/runeberry/ValheimServerGUI/wiki/Connecting-to-your-Server";
    public const string UrlDiscord = "https://discord.gg/HBsNJTY";
    public const string UrlReleases = "https://github.com/runeberry/ValheimServerGUI/releases";
    public const string UrlDonate = "https://www.buymeacoffee.com/runeberry";
    public const string UrlGitHub = "https://github.com/runeberry/ValheimServerGUI";

    /// <summary>FAQ entry explaining why character names may show wrong ("Character names wrong?" link).</summary>
    public const string UrlHelpCharacterNames = "https://github.com/runeberry/ValheimServerGUI/wiki/Frequently-Asked-Questions";

    /// <summary>The Valheim wiki's World Modifiers page (World Preferences "Valheim Wiki." link).</summary>
    public const string UrlValheimWikiWorldModifiers = "https://valheim.fandom.com/wiki/World_Modifiers";

    /// <summary>FAQ entry about world modifiers set in-game (World Preferences "Read more here." link).</summary>
    public const string UrlHelpWorldModifiers = "https://github.com/runeberry/ValheimServerGUI/wiki/Frequently-Asked-Questions";
}
