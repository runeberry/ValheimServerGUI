using System.Linq;
using ValheimServerGUI.Game;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Game
{
    /// <summary>
    /// Config defaulting + round-trip (E32, E36, E38, E39, E40 / §15 #7). The nullable "*File" model
    /// coalesces each missing key to the typed default; ToFile drops blank-named entries and de-dups.
    /// </summary>
    public class ConfigRoundTripTests
    {
        // E36 / E32: an empty file yields the typed defaults (from CoreConstants).
        [Fact]
        public void ServerPreferences_FromEmptyFile_UsesDefaults()
        {
            var prefs = ServerPreferences.FromFile(new ServerPreferencesFile());

            Assert.Equal(CoreConstants.DefaultServerProfileName, prefs.ProfileName);
            Assert.Equal(CoreConstants.DefaultServerPort, prefs.Port);
            Assert.Equal(CoreConstants.DefaultSaveInterval, prefs.SaveInterval);
            Assert.Equal(CoreConstants.DefaultBackupCount, prefs.BackupCount);
            Assert.Equal(CoreConstants.DefaultBackupIntervalShort, prefs.BackupIntervalShort);
            Assert.Equal(CoreConstants.DefaultBackupIntervalLong, prefs.BackupIntervalLong);
            Assert.True(prefs.WriteServerLogsToFile);
        }

        [Fact]
        public void ServerPreferences_FromNullFile_UsesDefaults()
        {
            var prefs = ServerPreferences.FromFile(null);
            Assert.Equal(CoreConstants.DefaultServerProfileName, prefs.ProfileName);
            Assert.Equal(CoreConstants.DefaultServerPort, prefs.Port);
        }

        [Fact]
        public void ServerPreferences_RoundTrip_PreservesValues()
        {
            var original = new ServerPreferences
            {
                ProfileName = "MyProfile",
                Name = "My Server",
                Password = "secret123",
                WorldName = "MyWorld",
                Public = true,
                Port = 2470,
                Crossplay = true,
                SaveInterval = 900,
                BackupCount = 7,
                AdditionalArgs = "-foo",
                WriteServerLogsToFile = false,
            };

            var restored = ServerPreferences.FromFile(original.ToFile());

            Assert.Equal("MyProfile", restored.ProfileName);
            Assert.Equal("My Server", restored.Name);
            Assert.Equal("secret123", restored.Password);
            Assert.Equal("MyWorld", restored.WorldName);
            Assert.True(restored.Public);
            Assert.Equal(2470, restored.Port);
            Assert.True(restored.Crossplay);
            Assert.Equal(900, restored.SaveInterval);
            Assert.Equal(7, restored.BackupCount);
            Assert.Equal("-foo", restored.AdditionalArgs);
            Assert.False(restored.WriteServerLogsToFile);
        }

        // Access-list feature: the permitted-list flag + per-player role map survive ToFile/FromFile, with
        // the role encoded as a lowercase string token and PlatformRaw preserved for non-Steam entries.
        [Fact]
        public void ServerPreferences_RoundTrip_PreservesRolesAndPermittedFlag()
        {
            var original = new ServerPreferences { ProfileName = "P", UsePermittedList = true };
            original.PlayerRoles["Steam:1"] = new PlayerRoleEntry(PlayerRole.Admin, null);
            original.PlayerRoles["Xbox:XUID"] = new PlayerRoleEntry(PlayerRole.Banned, "Xbox");
            original.PlayerRoles["Nintendo:N1"] = new PlayerRoleEntry(PlayerRole.Permitted, "Switch");

            var restored = ServerPreferences.FromFile(original.ToFile());

            Assert.True(restored.UsePermittedList);
            Assert.Equal(3, restored.PlayerRoles.Count);
            Assert.Equal(PlayerRole.Admin, restored.PlayerRoles["Steam:1"].Role);
            Assert.Equal(PlayerRole.Banned, restored.PlayerRoles["Xbox:XUID"].Role);
            Assert.Equal("Switch", restored.PlayerRoles["Nintendo:N1"].PlatformRaw);
        }

        // A profile that never touched the feature stays lean: no roles map serialized, flag defaults false.
        [Fact]
        public void ServerPreferences_EmptyRoles_AreOmittedFromFile_AndDefaultOnLoad()
        {
            var file = new ServerPreferences { ProfileName = "P" }.ToFile();
            Assert.Null(file.PlayerRoles);

            var restored = ServerPreferences.FromFile(new ServerPreferencesFile());
            Assert.False(restored.UsePermittedList);
            Assert.Empty(restored.PlayerRoles);
        }

        // E38 / E39: ToFile drops blank-named profiles and de-dups by profile name.
        [Fact]
        public void UserPreferences_ToFile_DropsBlankNamesAndDedups()
        {
            var prefs = new UserPreferences();
            prefs.Servers.Add(new ServerPreferences { ProfileName = "Dup" });
            prefs.Servers.Add(new ServerPreferences { ProfileName = "Dup" });
            prefs.Servers.Add(new ServerPreferences { ProfileName = "   " });

            var file = prefs.ToFile();

            Assert.Single(file.Servers!);
            Assert.Equal("Dup", file.Servers!.Single().ProfileName);
        }

        // §15 #7 / E40: a file with null "keys"/"modifiers" must not NPE; collections default to empty.
        [Fact]
        public void WorldPreferences_FromFileWithNullKeysAndModifiers_DoesNotThrow()
        {
            var prefs = WorldPreferences.FromFile(new WorldPreferencesFile
            {
                WorldName = "W",
                Keys = null,
                Modifiers = null,
            });

            Assert.NotNull(prefs.Keys);
            Assert.Empty(prefs.Keys);
            Assert.NotNull(prefs.Modifiers);
            Assert.Empty(prefs.Modifiers);
        }

        [Fact]
        public void WorldPreferences_RoundTrip_PreservesKeysAndModifiers()
        {
            var original = new WorldPreferences
            {
                WorldName = "W",
                Preset = WorldGenPresets.Hard,
                Keys = new() { WorldGenKeys.NoMap },
                Modifiers = new() { { WorldGenModifiers.Combat, WorldGenModifiers.Values.CombatHard } },
            };

            var restored = WorldPreferences.FromFile(original.ToFile());

            Assert.Equal("W", restored.WorldName);
            Assert.Equal(WorldGenPresets.Hard, restored.Preset);
            Assert.Contains(WorldGenKeys.NoMap, restored.Keys);
            Assert.Equal(WorldGenModifiers.Values.CombatHard, restored.Modifiers[WorldGenModifiers.Combat]);
        }

        // §16.2 enhancement: LastActiveProfile round-trips, and an empty file leaves it null (so the
        // startup selection falls back to most-recently-saved).
        [Fact]
        public void UserPreferences_LastActiveProfile_RoundTrips()
        {
            var prefs = new UserPreferences { LastActiveProfile = "Nightshade" };
            var restored = UserPreferences.FromFile(prefs.ToFile());
            Assert.Equal("Nightshade", restored.LastActiveProfile);
        }

        [Fact]
        public void UserPreferences_LastActiveProfile_DefaultsNull()
        {
            Assert.Null(UserPreferences.FromFile(new UserPreferencesFile()).LastActiveProfile);
        }

        // §16.2 theme enhancement: round-trips, defaults to System.
        [Fact]
        public void UserPreferences_Theme_RoundTrips()
        {
            var restored = UserPreferences.FromFile(new UserPreferences { Theme = AppTheme.Dark }.ToFile());
            Assert.Equal(AppTheme.Dark, restored.Theme);
        }

        [Fact]
        public void UserPreferences_Theme_DefaultsToSystem()
        {
            Assert.Equal(AppTheme.System, UserPreferences.FromFile(new UserPreferencesFile()).Theme);
        }
    }
}
