using ValheimServerGUI.Localization;

namespace ValheimServerGUI.App;

/// <summary>Shell-level constants (product identity used by the OS-integration seams).</summary>
internal static class AppConstants
{
    /// <summary>Product name used for the window title, splash, About header, autostart entries, and the tray tooltip.</summary>
    public const string ProductName = "ValheimServerGUI";

    /// <summary>Stable identifier used as the autostart registration key / <c>.desktop</c> file name.</summary>
    public const string StartupKey = "ValheimServerGUI";

    /// <summary>
    /// View-only marker for a world that lives only in Steam Cloud. Applied and stripped exclusively by the
    /// world-select surface — it never reaches saved prefs, server options, or validation.
    /// Localized, so it is read from Strings rather than being a compile-time constant.
    /// </summary>
    public static string CloudWorldSuffix => Strings.ServerControls_CloudWorldSuffix;

    /// <summary>Root of the online manual (the vsg-docs site). Every help link below is derived from it.</summary>
    public const string UrlDocs = "https://runeberry.com/valheim-server-gui/";

    // External links.
    public const string UrlHelp = UrlDocs;
    public const string UrlPortForwarding = UrlDocs + "docs/connecting-to-your-server/";
    public const string UrlDiscord = "https://discord.gg/HBsNJTY";
    /// <summary>Owned tip link; runeberry.com redirects it to the current tip page, so it never needs an app update.</summary>
    public const string UrlTipJar = "https://runeberry.com/tips";
    public const string UrlSourceCode = "https://forge.nuffle.me/runeberry/ValheimServerGUI";
    public const string UrlValheimGameSite = "https://www.valheimgame.com/";

    /// <summary>FAQ entry explaining why character names may show wrong ("Character names wrong?" link).</summary>
    public const string UrlHelpCharacterNames =
        UrlDocs + "docs/frequently-asked-questions/#valheimservergui-matched-a-character-name-with-the-wrong-player-how-do-i-fix-this";

    /// <summary>The Valheim wiki's World Modifiers page (World Preferences "Valheim Wiki." link).</summary>
    public const string UrlValheimWikiWorldModifiers = "https://valheim.fandom.com/wiki/World_Modifiers";

    /// <summary>FAQ entry about world modifiers set in-game (World Preferences "Read more here." link).</summary>
    public const string UrlHelpWorldModifiers =
        UrlDocs + "docs/frequently-asked-questions/#valheimservergui-isnt-correctly-displaying-my-world-modifiers";
}
