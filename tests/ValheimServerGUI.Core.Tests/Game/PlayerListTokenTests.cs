using ValheimServerGUI.Game;
using ValheimServerGUI.Tools.Models;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Game
{
    /// <summary>
    /// Table over <see cref="PlayerListToken.TryResolve"/>: prefixed tokens resolve by platform (raw prefix
    /// preserved, Steam normalized), a bare steam64-shaped id resolves to Steam, and everything ambiguous
    /// fails closed.
    /// </summary>
    public class PlayerListTokenTests
    {
        [Theory]
        // Prefixed forms: platform normalized, platformRaw = exact prefix (except Steam → "Steam").
        [InlineData("Steam_76561198000000001", PlayerPlatforms.Steam, "Steam", "76561198000000001")]
        [InlineData("steam_123", PlayerPlatforms.Steam, "Steam", "123")]
        [InlineData("Xbox_abc123", PlayerPlatforms.Xbox, "Xbox", "abc123")]
        [InlineData("Playstation_pqr", PlayerPlatforms.PlayStation, "Playstation", "pqr")]
        [InlineData("PlayStation_pqr", PlayerPlatforms.PlayStation, "PlayStation", "pqr")]
        [InlineData("Switch_zzz", PlayerPlatforms.Nintendo, "Switch", "zzz")]
        // Bare steam64-shaped id → Steam.
        [InlineData("76561198000000001", PlayerPlatforms.Steam, "Steam", "76561198000000001")]
        public void Resolves_recognized_tokens(string token, string platform, string platformRaw, string playerId)
        {
            Assert.True(PlayerListToken.TryResolve(token, out var p, out var raw, out var id));
            Assert.Equal(platform, p);
            Assert.Equal(platformRaw, raw);
            Assert.Equal(playerId, id);
        }

        [Theory]
        [InlineData("123456")]                 // bare, not steam64-shaped
        [InlineData("7656119")]                // too short for steam64
        [InlineData("765611980000000010")]     // too long for steam64 (18 digits)
        [InlineData("Origin_abc")]             // unknown prefix
        [InlineData("Steam_123_456")]          // id carries a further '_'
        [InlineData("Steam_")]                 // empty id
        [InlineData("_123")]                   // empty prefix
        [InlineData("")]                       // empty
        [InlineData("   ")]                    // whitespace
        public void Fails_closed_on_ambiguous_tokens(string token)
        {
            Assert.False(PlayerListToken.TryResolve(token, out var p, out var raw, out var id));
            Assert.Equal(string.Empty, p);
            Assert.Equal(string.Empty, raw);
            Assert.Equal(string.Empty, id);
        }
    }
}
