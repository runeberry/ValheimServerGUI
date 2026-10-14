namespace ValheimServerGUI.Game
{
    /// <summary>
    /// A player's app-global default: the role applied on every server the user hosts unless that server's
    /// profile overrides it (see <see cref="PlayerRoleResolver"/>). A player with no default has no entry —
    /// <see cref="PlayerRole.None"/> is never stored here. A <see cref="PlayerRole.Banned"/> default is what puts
    /// a player on the Manage Players "Banned" list.
    /// </summary>
    /// <param name="DefaultRole">The role applied on every server unless overridden (never None).</param>
    /// <param name="PlatformRaw">The raw platform token for a case-exact list-file entry (null = normalized platform).</param>
    public record PlayerDefaultEntry(PlayerRole DefaultRole, string? PlatformRaw);
}
