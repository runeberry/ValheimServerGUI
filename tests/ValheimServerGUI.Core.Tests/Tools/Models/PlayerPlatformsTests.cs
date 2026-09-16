using ValheimServerGUI.Tools.Models;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Tools.Models
{
    /// <summary>
    /// Backs the crossplay player-correlation path (E21) and list management: Steam/Xbox/PlayStation/Nintendo
    /// are valid platforms (the game's "Switch" token normalizes to Nintendo), input is case-corrected and
    /// trimmed, and anything else (or null) is rejected so it is never recorded.
    /// </summary>
    public class PlayerPlatformsTests
    {
        [Theory]
        [InlineData("steam", PlayerPlatforms.Steam)]
        [InlineData("STEAM", PlayerPlatforms.Steam)]
        [InlineData("  Steam  ", PlayerPlatforms.Steam)]
        [InlineData("xbox", PlayerPlatforms.Xbox)]
        [InlineData("Xbox", PlayerPlatforms.Xbox)]
        [InlineData("playstation", PlayerPlatforms.PlayStation)]
        [InlineData("PlayStation", PlayerPlatforms.PlayStation)]
        [InlineData("nintendo", PlayerPlatforms.Nintendo)]
        [InlineData("switch", PlayerPlatforms.Nintendo)]
        [InlineData("  Switch  ", PlayerPlatforms.Nintendo)]
        public void TryGetValidPlatform_KnownPlatform_ReturnsCaseCorrectedName(string input, string expected)
        {
            var ok = PlayerPlatforms.TryGetValidPlatform(input, out var platform);

            Assert.True(ok);
            Assert.Equal(expected, platform);
        }

        [Theory]
        [InlineData("Epic")]
        [InlineData("GamePass")]
        [InlineData("")]
        [InlineData(null)]
        public void TryGetValidPlatform_UnknownOrNull_Fails(string? input)
        {
            var ok = PlayerPlatforms.TryGetValidPlatform(input, out var platform);

            Assert.False(ok);
            Assert.Null(platform);
        }

        [Fact]
        public void All_ContainsTheFourSupportedPlatforms()
        {
            Assert.Equal(4, PlayerPlatforms.All.Count);
            Assert.Contains(PlayerPlatforms.Steam, PlayerPlatforms.All);
            Assert.Contains(PlayerPlatforms.Xbox, PlayerPlatforms.All);
            Assert.Contains(PlayerPlatforms.PlayStation, PlayerPlatforms.All);
            Assert.Contains(PlayerPlatforms.Nintendo, PlayerPlatforms.All);
        }
    }
}
