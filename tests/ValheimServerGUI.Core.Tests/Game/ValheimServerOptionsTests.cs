using System;
using System.Collections.Generic;
using ValheimServerGUI.Game;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Game;

/// <summary>
/// The world-gen validation messages are shown to the user verbatim when a server fails to start. They name
/// the rejected value and the supported values, with no stray template characters.
/// </summary>
public class ValheimServerOptionsTests
{
    // Valid up to the world-gen checks, which run before the filesystem checks.
    private static ValheimServerOptions Options(Action<ValheimServerOptions> customize)
    {
        var options = new ValheimServerOptions
        {
            Name = "My Server",
            WorldName = "MyWorld",
            Password = "hunter2",
            Port = 2456,
            SaveInterval = 30,
            Backups = 1,
            BackupShort = 60,
            BackupLong = 120,
        };
        customize(options);
        return options;
    }

    public static IEnumerable<object[]> InvalidWorldGen() => new[]
    {
        new object[] { (Action<ValheimServerOptions>)(o => o.WorldPreset = "bogus"), "bogus", WorldGenPresets.Normal },
        new object[] { (Action<ValheimServerOptions>)(o => o.WorldModifiers = new() { ["bogus"] = "x" }), "bogus", WorldGenModifiers.Combat },
        new object[] { (Action<ValheimServerOptions>)(o => o.WorldModifiers = new() { [WorldGenModifiers.Combat] = "bogus" }), "bogus", WorldGenModifiers.Values.CombatHard },
        new object[] { (Action<ValheimServerOptions>)(o => o.WorldKeys = new() { "bogus" }), "bogus", WorldGenKeys.NoMap },
    };

    [Theory]
    [MemberData(nameof(InvalidWorldGen))]
    public void Invalid_world_gen_message_names_the_value_and_the_supported_values(
        Action<ValheimServerOptions> customize, string rejected, string supported)
    {
        var ex = Assert.Throws<ArgumentException>(() => Options(customize).Validate());

        Assert.Contains(rejected, ex.Message);
        Assert.Contains(supported, ex.Message);
        Assert.DoesNotContain("$", ex.Message);
    }
}
