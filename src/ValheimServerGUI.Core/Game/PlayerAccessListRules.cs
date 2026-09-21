using System.Collections.Generic;

namespace ValheimServerGUI.Game
{
    /// <summary>
    /// The single source of truth for how a stored <see cref="PlayerRole"/> maps onto the three Valheim
    /// gating files, given the profile's <c>usePermittedList</c> flag. Both valid Valheim configurations are
    /// encoded here so file generation (<see cref="PlayerAccessListService.GenerateFiles"/>) and the UI's
    /// effective-role display read the same rules:
    ///
    /// <list type="bullet">
    /// <item><b>usePermittedList = false</b> — anyone joins unless banned; admins get commands.
    /// Admin → adminlist; Banned → bannedlist; Permitted → (nothing, the list is unused).</item>
    /// <item><b>usePermittedList = true</b> — only permitted players join; the ban list is ignored; an admin
    /// must be on <i>both</i> lists to join and have commands.
    /// Admin → adminlist + permittedlist; Permitted → permittedlist; Banned → (nothing).</item>
    /// </list>
    /// </summary>
    public static class PlayerAccessListRules
    {
        /// <summary>The list files a player with <paramref name="role"/> should appear in for the given mode.</summary>
        public static IEnumerable<PlayerAccessList> TargetLists(PlayerRole role, bool usePermittedList)
        {
            switch (role)
            {
                case PlayerRole.Admin:
                    yield return PlayerAccessList.Admin;
                    // In permitted-list mode an admin must also be permitted, or the game keeps them out.
                    if (usePermittedList) yield return PlayerAccessList.Permitted;
                    break;

                case PlayerRole.Permitted:
                    // The permitted list only does anything in permitted-list mode.
                    if (usePermittedList) yield return PlayerAccessList.Permitted;
                    break;

                case PlayerRole.Banned:
                    // The ban list is ignored in permitted-list mode (join is decided by the permitted list).
                    if (!usePermittedList) yield return PlayerAccessList.Banned;
                    break;
            }
        }
    }
}
