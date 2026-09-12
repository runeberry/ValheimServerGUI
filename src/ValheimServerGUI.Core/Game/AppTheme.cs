namespace ValheimServerGUI.Game
{
    /// <summary>
    /// The user's UI theme preference (§16.2 enhancement). <see cref="System"/> follows the OS theme
    /// variant; the shell maps this to Avalonia's <c>RequestedThemeVariant</c>.
    /// </summary>
    public enum AppTheme
    {
        System = 0,
        Light = 1,
        Dark = 2,
    }
}
