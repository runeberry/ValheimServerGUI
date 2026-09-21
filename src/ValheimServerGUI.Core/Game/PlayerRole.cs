namespace ValheimServerGUI.Game
{
    /// <summary>
    /// The single role VSG stores for a player within a server profile. The absence of an entry (no role)
    /// is represented by <c>null</c> everywhere a role is optional — this enum has no "unset" member.
    /// The three Valheim gating files are <b>generated</b> from these roles + the profile's
    /// <c>UsePermittedList</c> flag at server start (see <see cref="PlayerAccessListRules"/>), rather than
    /// being edited directly.
    /// </summary>
    public enum PlayerRole
    {
        Admin,
        Permitted,
        Banned,
    }

    /// <summary>A profile's stored role for one player, plus the raw platform token needed to write a
    /// case-exact non-Steam list-file entry (falls back to the normalized platform when null).</summary>
    public record PlayerRoleEntry(PlayerRole Role, string? PlatformRaw);
}
