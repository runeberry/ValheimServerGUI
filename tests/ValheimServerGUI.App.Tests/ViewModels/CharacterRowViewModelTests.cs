using System;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.Game;
using Xunit;

namespace ValheimServerGUI.App.Tests.ViewModels;

// Per-character Status/Since are derived from the owning player: only the active character carries the
// live status, and "Since" is blank when the (optional) LastSeen wasn't recorded (legacy data).
public class CharacterRowViewModelTests
{
    private static PlayerInfo Player(PlayerStatus status, string active) => new()
    {
        Platform = "Steam", PlayerId = "1", PlayerStatus = status, LastStatusCharacter = active,
    };

    [Fact]
    public void Active_character_carries_the_live_status()
    {
        var row = new CharacterRowViewModel("Odin", lastSeen: new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero));
        row.Refresh(Player(PlayerStatus.Online, "Odin"), new DateTimeOffset(2026, 9, 15, 12, 5, 0, TimeSpan.Zero));

        Assert.Equal(PlayerStatus.Online, row.Status);
        Assert.Equal("5 minutes ago", row.SinceText);
    }

    [Fact]
    public void Non_active_character_is_offline()
    {
        var row = new CharacterRowViewModel("Thor");
        row.Refresh(Player(PlayerStatus.Online, "Odin"), DateTimeOffset.Now);

        Assert.Equal(PlayerStatus.Offline, row.Status);
    }

    [Fact]
    public void Missing_last_seen_leaves_since_blank()
    {
        var row = new CharacterRowViewModel("Odin"); // no LastSeen (legacy)
        row.Refresh(Player(PlayerStatus.Offline, "Odin"), DateTimeOffset.Now);

        Assert.Equal(string.Empty, row.SinceText);
    }
}
