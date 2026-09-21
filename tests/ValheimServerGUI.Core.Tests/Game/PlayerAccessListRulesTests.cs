using System.Linq;
using ValheimServerGUI.Game;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Game
{
    /// <summary>
    /// The role→files mapping table, both modes. This is the single rules source that file generation and the
    /// UI's effective-role display both read, so all six (role × usePermittedList) combinations are pinned.
    /// </summary>
    public class PlayerAccessListRulesTests
    {
        // usePermittedList = false: admin→admin; banned→banned; permitted→(nothing).
        [Theory]
        [InlineData(PlayerRole.Admin, new[] { PlayerAccessList.Admin })]
        [InlineData(PlayerRole.Banned, new[] { PlayerAccessList.Banned })]
        [InlineData(PlayerRole.Permitted, new PlayerAccessList[0])]
        public void OpenMode_MapsRoleToLists(PlayerRole role, PlayerAccessList[] expected)
        {
            Assert.Equal(expected, PlayerAccessListRules.TargetLists(role, usePermittedList: false).ToArray());
        }

        // usePermittedList = true: admin→admin+permitted; permitted→permitted; banned→(nothing).
        [Theory]
        [InlineData(PlayerRole.Admin, new[] { PlayerAccessList.Admin, PlayerAccessList.Permitted })]
        [InlineData(PlayerRole.Permitted, new[] { PlayerAccessList.Permitted })]
        [InlineData(PlayerRole.Banned, new PlayerAccessList[0])]
        public void PermittedMode_MapsRoleToLists(PlayerRole role, PlayerAccessList[] expected)
        {
            Assert.Equal(expected, PlayerAccessListRules.TargetLists(role, usePermittedList: true).ToArray());
        }
    }
}
