using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using ValheimServerGUI.Game;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Game;

// The per-character LastSeen field (v3.0, additive) backs the Player Details "Since" column. It must be
// optional in the cache: characters written by earlier versions have no "lastSeen" and must load as null.
public class PlayerInfoTests
{
    [Fact]
    public void TouchCharacterLastSeen_sets_the_matching_character_only()
    {
        var player = new PlayerInfo { Platform = "Steam", PlayerId = "1" };
        player.AddCharacter("Odin");
        player.AddCharacter("Thor");
        var when = DateTimeOffset.UtcNow;

        player.TouchCharacterLastSeen("Odin", when);

        Assert.Equal(when, player.Characters!.Single(c => c.CharacterName == "Odin").LastSeen);
        Assert.Null(player.Characters!.Single(c => c.CharacterName == "Thor").LastSeen);
    }

    [Fact]
    public void TouchCharacterLastSeen_is_a_noop_for_unknown_or_empty_names()
    {
        var player = new PlayerInfo { Platform = "Steam", PlayerId = "1" };
        player.AddCharacter("Odin");

        player.TouchCharacterLastSeen("Nobody", DateTimeOffset.UtcNow);
        player.TouchCharacterLastSeen(null, DateTimeOffset.UtcNow);

        Assert.Null(player.Characters!.Single().LastSeen);
    }

    [Fact]
    public void LastSeen_round_trips_through_json()
    {
        var when = new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);
        var player = new PlayerInfo
        {
            Platform = "Steam",
            PlayerId = "1",
            Characters = new List<PlayerInfo.CharacterInfo> { new() { CharacterName = "Odin", LastSeen = when } },
        };

        var json = JsonConvert.SerializeObject(player);
        var loaded = JsonConvert.DeserializeObject<PlayerInfo>(json)!;

        Assert.Equal(when, loaded.Characters!.Single().LastSeen);
    }

    [Fact]
    public void Legacy_character_without_lastSeen_loads_as_null()
    {
        // A cache entry written before the field existed: no "lastSeen" key.
        const string legacy =
            "{\"platform\":\"Steam\",\"playerId\":\"1\"," +
            "\"characters\":[{\"characterName\":\"Odin\",\"matchConfident\":true}]}";

        var loaded = JsonConvert.DeserializeObject<PlayerInfo>(legacy)!;
        var character = loaded.Characters!.Single();

        Assert.Equal("Odin", character.CharacterName);
        Assert.Null(character.LastSeen);
    }

    [Fact]
    public void Null_lastSeen_is_omitted_from_serialized_output()
    {
        var player = new PlayerInfo
        {
            Platform = "Steam",
            PlayerId = "1",
            Characters = new List<PlayerInfo.CharacterInfo> { new() { CharacterName = "Odin" } },
        };

        var json = JsonConvert.SerializeObject(player);

        Assert.DoesNotContain("lastSeen", json);
    }
}
