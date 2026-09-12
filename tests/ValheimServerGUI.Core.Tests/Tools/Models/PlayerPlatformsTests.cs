using ValheimServerGUI.Tools.Models;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Tools.Models
{
    /// <summary>
    /// Backs the crossplay player-correlation path (E21): only Steam/Xbox are valid platforms, input
    /// is case-corrected and trimmed, and anything else (or null) is rejected so it is never recorded.
    /// </summary>
    public class PlayerPlatformsTests
    {
        [Theory]
        [InlineData("steam", PlayerPlatforms.Steam)]
        [InlineData("STEAM", PlayerPlatforms.Steam)]
        [InlineData("  Steam  ", PlayerPlatforms.Steam)]
        [InlineData("xbox", PlayerPlatforms.Xbox)]
        [InlineData("Xbox", PlayerPlatforms.Xbox)]
        public void TryGetValidPlatform_KnownPlatform_ReturnsCaseCorrectedName(string input, string expected)
        {
            var ok = PlayerPlatforms.TryGetValidPlatform(input, out var platform);

            Assert.True(ok);
            Assert.Equal(expected, platform);
        }

        [Theory]
        [InlineData("playstation")]
        [InlineData("")]
        [InlineData(null)]
        public void TryGetValidPlatform_UnknownOrNull_Fails(string? input)
        {
            var ok = PlayerPlatforms.TryGetValidPlatform(input, out var platform);

            Assert.False(ok);
            Assert.Null(platform);
        }
    }
}
