using System.Collections.Generic;
using System.Linq;
using ValheimServerGUI.Game;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Game
{
    /// <summary>Effective-role precedence: server override ?? global default ?? None.</summary>
    public class PlayerRoleResolverTests
    {
        private static Dictionary<string, PlayerRoleEntry> Overrides(params (string key, PlayerRole role)[] entries)
            => entries.ToDictionary(e => e.key, e => new PlayerRoleEntry(e.role, "Steam"));

        private static Dictionary<string, PlayerDefaultEntry> Defaults(params (string key, PlayerRole role)[] entries)
            => entries.ToDictionary(e => e.key, e => new PlayerDefaultEntry(e.role, "Steam"));

        [Fact]
        public void Override_wins_over_default()
        {
            var r = PlayerRoleResolver.Resolve("Steam:1",
                Overrides(("Steam:1", PlayerRole.Banned)),
                Defaults(("Steam:1", PlayerRole.Permitted)));

            Assert.Equal(PlayerRole.Banned, r.Effective);
            Assert.True(r.HasOverride);
            Assert.True(r.HasDefault);
        }

        [Fact]
        public void Default_applies_without_override()
        {
            var r = PlayerRoleResolver.Resolve("Steam:1", Overrides(),
                Defaults(("Steam:1", PlayerRole.Admin)));

            Assert.Equal(PlayerRole.Admin, r.Effective);
            Assert.False(r.HasOverride);
            Assert.False(r.ShowsOverrideMarker);
        }

        [Fact]
        public void Neither_resolves_to_None()
        {
            var r = PlayerRoleResolver.Resolve("Steam:1", Overrides(), Defaults());

            Assert.Equal(PlayerRole.None, r.Effective);
            Assert.False(r.HasOverride);
            Assert.False(r.HasDefault);
        }

        [Fact]
        public void Pinned_override_survives_a_default_change()
        {
            var overrides = Overrides(("Steam:1", PlayerRole.Permitted));
            var before = PlayerRoleResolver.Resolve("Steam:1", overrides,
                Defaults(("Steam:1", PlayerRole.Permitted)));
            var after = PlayerRoleResolver.Resolve("Steam:1", overrides,
                Defaults(("Steam:1", PlayerRole.Admin)));

            Assert.Equal(PlayerRole.Permitted, before.Effective);
            Assert.Equal(PlayerRole.Permitted, after.Effective);
            Assert.True(before.ShowsOverrideMarker); // equal-to-default override still counts as a pin
        }

        [Fact]
        public void Marker_needs_both_an_override_and_a_default()
        {
            var overrideOnly = PlayerRoleResolver.Resolve("Steam:1", Overrides(("Steam:1", PlayerRole.Admin)), Defaults());
            Assert.False(overrideOnly.ShowsOverrideMarker);
        }

        [Fact]
        public void BuildAssignments_unions_both_layers_with_overrides_winning()
        {
            var assignments = PlayerRoleResolver.BuildAssignments(
                Overrides(("Steam:1", PlayerRole.Banned), ("Steam:2", PlayerRole.Permitted), ("Xbox:9", PlayerRole.Admin)),
                Defaults(
                    ("Steam:2", PlayerRole.Admin),
                    ("Steam:3", PlayerRole.Permitted)));

            Assert.Equal(
                new (string?, string?, PlayerRole)[]
                {
                    ("Steam", "1", PlayerRole.Banned), ("Steam", "2", PlayerRole.Permitted),
                    ("Steam", "3", PlayerRole.Permitted), ("Xbox", "9", PlayerRole.Admin),
                },
                assignments.Select(a => (a.Platform, a.PlayerId, a.Role)).ToArray());
        }
    }
}
