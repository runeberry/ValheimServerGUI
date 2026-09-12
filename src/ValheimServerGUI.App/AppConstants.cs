namespace ValheimServerGUI.App;

/// <summary>Shell-level constants (product identity used by the OS-integration seams).</summary>
internal static class AppConstants
{
    /// <summary>Product name used for the window title, autostart entries, and the tray tooltip.</summary>
    public const string ProductName = "Valheim Server GUI";

    /// <summary>Stable identifier used as the autostart registration key / <c>.desktop</c> file name.</summary>
    public const string StartupKey = "ValheimServerGUI";
}
