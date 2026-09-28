using System.Collections.Generic;
using ValheimServerGUI.App.Tests.Fakes;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.App.ViewModels.Dialogs;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools.Models;
using Xunit;

namespace ValheimServerGUI.App.Tests.ViewModels;

/// <summary>The one add-a-player routine shared by the Players tab and both Manage Players tables.</summary>
public class AddPlayerFlowTests
{
    private readonly FakePlayerDataRepository _repo = new();
    private readonly Dictionary<string, PlayerDefaultEntry> _defaults = new();
    private readonly Dictionary<string, PlayerRole?> _overrides = new();

    private AddPlayerFlow.Outcome? Add(PlayerRole role, bool asDefault, bool fromServer = true, string? name = null)
        => AddPlayerFlow.Apply(
            new AddPlayerResult(PlayerPlatforms.Steam, " 42 ", name, role, asDefault),
            new RepoPlayerRecordStore(_repo), _defaults,
            fromServer ? (player, over) => _overrides[player.Key] = over : null);

    [Fact]
    public void Creates_the_record_and_reports_it_new()
    {
        var outcome = Add(PlayerRole.Permitted, asDefault: true, name: "Thor")!;

        Assert.True(outcome.IsNewRecord);
        Assert.Equal("Thor", _repo.FindById("Steam:42")!.PlayerName);
        Assert.False(Add(PlayerRole.Permitted, asDefault: true)!.IsNewRecord);
        Assert.Equal("Thor", _repo.FindById("Steam:42")!.PlayerName); // blank name keeps the cached one
    }

    [Fact]
    public void As_default_sets_the_default_and_clears_this_servers_override()
    {
        Add(PlayerRole.Admin, asDefault: true);

        Assert.Equal(new PlayerDefaultEntry(PlayerRole.Admin, "Steam"), _defaults["Steam:42"]);
        Assert.Null(_overrides["Steam:42"]);
    }

    [Fact]
    public void Default_None_clears_the_default_role()
    {
        _defaults["Steam:42"] = new PlayerDefaultEntry(PlayerRole.Banned, "Steam");

        Add(PlayerRole.None, asDefault: true, fromServer: false);

        Assert.False(_defaults.ContainsKey("Steam:42"));
    }

    [Fact]
    public void Server_role_sets_an_override_and_leaves_the_default()
    {
        _defaults["Steam:42"] = new PlayerDefaultEntry(PlayerRole.Permitted, "Steam");

        Add(PlayerRole.Banned, asDefault: false);

        Assert.Equal(PlayerRole.Banned, _overrides["Steam:42"]);
        Assert.Equal(PlayerRole.Permitted, _defaults["Steam:42"].DefaultRole);
    }

    [Fact]
    public void Server_role_None_removes_the_override_and_the_default_applies()
    {
        _defaults["Steam:42"] = new PlayerDefaultEntry(PlayerRole.Admin, "Steam");

        Add(PlayerRole.None, asDefault: false);

        Assert.Null(_overrides["Steam:42"]); // never a "no role" override
        Assert.Equal(PlayerRole.Admin, _defaults["Steam:42"].DefaultRole);
    }

    [Fact]
    public void Without_a_server_the_role_always_applies_as_a_default()
    {
        Add(PlayerRole.Banned, asDefault: false, fromServer: false);

        Assert.Equal(PlayerRole.Banned, _defaults["Steam:42"].DefaultRole);
    }

    [Fact]
    public void Invalid_platform_or_id_does_nothing()
    {
        var store = new RepoPlayerRecordStore(_repo);
        Assert.Null(AddPlayerFlow.Apply(new AddPlayerResult("Dreamcast", "1", null, PlayerRole.Admin, true), store, _defaults, null));
        Assert.Null(AddPlayerFlow.Apply(new AddPlayerResult(PlayerPlatforms.Steam, " ", null, PlayerRole.Admin, true), store, _defaults, null));
        Assert.Empty(_repo.Data);
    }
}
