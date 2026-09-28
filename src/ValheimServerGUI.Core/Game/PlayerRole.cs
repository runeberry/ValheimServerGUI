namespace ValheimServerGUI.Game
{
    /// <summary>
    /// The single role a player holds on a server. Roles come from two layers (see
    /// <see cref="PlayerRoleResolver"/>): an app-global <b>default</b> (the Manage Players lists) and a per-server
    /// <b>override</b> stored on the profile. The three Valheim gating files are <b>generated</b> from the resolved
    /// roles + the profile's <c>UsePermittedList</c> flag at server start (see <see cref="PlayerAccessListRules"/>),
    /// rather than being edited directly.
    /// </summary>
    public enum PlayerRole
    {
        Admin,
        Permitted,
        Banned,

        /// <summary>
        /// Explicitly no role. As an override it pins "no role" on one server even when the player's global
        /// default grants one; it never appears in any list file.
        /// </summary>
        None,
    }

    /// <summary>A stored role for one player, plus the raw platform token needed to write a case-exact non-Steam
    /// list-file entry (falls back to the normalized platform when null).</summary>
    public record PlayerRoleEntry(PlayerRole Role, string? PlatformRaw);

    /// <summary>The lowercase string tokens a <see cref="PlayerRole"/> is persisted as (stable across enum
    /// reorders). Shared by the server-profile overrides and the global player defaults.</summary>
    public static class PlayerRoleTokens
    {
        private const string Admin = "admin";
        private const string Permitted = "permitted";
        private const string Banned = "banned";
        private const string None = "none";

        public static string ToToken(PlayerRole role) => role switch
        {
            PlayerRole.Admin => Admin,
            PlayerRole.Permitted => Permitted,
            PlayerRole.Banned => Banned,
            _ => None,
        };

        /// <summary>Parses a persisted token; unknown values fail so the caller can drop the entry.</summary>
        public static bool TryParse(string? value, out PlayerRole role)
        {
            switch (value?.Trim().ToLowerInvariant())
            {
                case Admin: role = PlayerRole.Admin; return true;
                case Permitted: role = PlayerRole.Permitted; return true;
                case Banned: role = PlayerRole.Banned; return true;
                case None: role = PlayerRole.None; return true;
                default: role = default; return false;
            }
        }
    }
}
