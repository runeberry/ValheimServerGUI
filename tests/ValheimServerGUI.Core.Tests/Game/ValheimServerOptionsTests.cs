using System;
using System.Collections.Generic;
using System.IO;
using ValheimServerGUI.Game;
using ValheimServerGUI.Localization;
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

    // Environment variables are checked before the filesystem checks, so these options need no real paths.

    [Theory]
    [InlineData("SteamAppId=1")]
    [InlineData("steamappid=1")]
    [InlineData("A=1 SteamAppId=1")]
    public void SteamAppId_is_always_rejected(string variables)
    {
        var ex = Assert.Throws<ArgumentException>(() => Options(o => o.EnvironmentVariables = variables).Validate());

        Assert.Contains(ex.Message, new[]
        {
            string.Format(Strings.Validation_EnvVarSetByApp, "SteamAppId"),
            string.Format(Strings.Validation_EnvVarSetByApp, "steamappid"),
        });
    }

    [Fact]
    public void Malformed_environment_variables_fail_validation_with_the_parser_message()
    {
        var ex = Assert.Throws<ArgumentException>(() => Options(o => o.EnvironmentVariables = "BROKEN").Validate());

        Assert.Equal(string.Format(Strings.Validation_EnvVarMissingEquals, "BROKEN"), ex.Message);
    }

    // The Doorstop variables are VSG's only while VSG is loading BepInEx (Linux, both files present); otherwise a
    // hand-rolled Doorstop setup may set them, and validation moves on to the next check (here: the missing exe).
    [Theory]
    [InlineData("DOORSTOP_ENABLED=0")]
    [InlineData("DOORSTOP_TARGET_ASSEMBLY=/x.dll")]
    public void Doorstop_variables_are_rejected_only_while_VSG_loads_BepInEx(string variables)
    {
        var dir = Directory.CreateTempSubdirectory("vsg-opts-");
        try
        {
            var preloader = Path.Join(dir.FullName, "BepInEx.Preloader.dll");
            var doorstop = Path.Join(dir.FullName, "libdoorstop_x64.so");
            File.WriteAllText(preloader, "");

            ArgumentException Validate() => Assert.Throws<ArgumentException>(() => Options(o =>
            {
                o.EnvironmentVariables = variables;
                o.BepInExPreloaderPath = preloader;
                o.DoorstopLibraryPath = doorstop;
            }).Validate());

            var name = variables.Split('=')[0];
            var bepInExMessage = string.Format(Strings.Validation_EnvVarSetForBepInEx, name);

            Assert.NotEqual(bepInExMessage, Validate().Message); // the .so is missing: VSG is not loading BepInEx

            File.WriteAllText(doorstop, "");
            if (OperatingSystem.IsWindows())
                Assert.NotEqual(bepInExMessage, Validate().Message); // never VSG's on Windows
            else
                Assert.Equal(bepInExMessage, Validate().Message);
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }
}
