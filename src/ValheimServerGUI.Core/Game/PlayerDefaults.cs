namespace ValheimServerGUI.Game
{
    /// <summary>Which Manage Players list an account sits on.</summary>
    public enum PlayerCategory
    {
        MyAccount,
        Friend,
        Banned,
    }

    /// <summary>
    /// An account's app-global record: the list it sits on plus the role it gets by default on every server
    /// (a per-server override in <see cref="ServerPreferences.PlayerRoles"/> wins over it). Banned-list entries
    /// always carry <see cref="PlayerRole.Banned"/>.
    /// </summary>
    /// <param name="Category">The list the account sits on.</param>
    /// <param name="DefaultRole">The role applied on every server unless overridden.</param>
    /// <param name="PlatformRaw">The raw platform token for a case-exact list-file entry (null = normalized platform).</param>
    public record PlayerDefaultEntry(PlayerCategory Category, PlayerRole DefaultRole, string? PlatformRaw)
    {
        /// <summary>Enforces the category invariant: a Banned-list entry's default role is always Banned.</summary>
        public PlayerDefaultEntry Normalized()
            => Category == PlayerCategory.Banned && DefaultRole != PlayerRole.Banned
                ? this with { DefaultRole = PlayerRole.Banned }
                : this;
    }
}
