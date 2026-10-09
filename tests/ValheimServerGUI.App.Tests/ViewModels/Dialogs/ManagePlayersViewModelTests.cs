using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using ValheimServerGUI.App.Tests.Fakes;
using ValheimServerGUI.App.ViewModels.Dialogs;
using ValheimServerGUI.Game;
using ValheimServerGUI.Localization;
using ValheimServerGUI.Tools.Models;
using Xunit;

namespace ValheimServerGUI.App.Tests.ViewModels.Dialogs;

public class ManagePlayersViewModelTests
{
    private readonly FakeUserPreferencesProvider _prefs = new();
    private readonly FakePlayerDataRepository _repo = new();
    private readonly FakeRuneberryApiClient _api = new();
    private readonly List<AddPlayerOptions> _offered = new();

    private ManagePlayersViewModel NewVm(params (string key, PlayerRole role)[] defaults)
    {
        if (defaults.Length > 0)
        {
            var prefs = new UserPreferences();
            foreach (var (key, role) in defaults)
                prefs.PlayerDefaults[key] = new PlayerDefaultEntry(role, "Steam");
            _prefs.SavePreferences(prefs);
        }
        return new ManagePlayersViewModel(_prefs, _repo, _api);
    }

    // Makes the next Add Player dialog return the given result, recording the options it was opened with.
    private void NextAdd(ManagePlayersViewModel vm, string id, PlayerRole role, string? name = null)
        => vm.AddPlayerPrompt = options =>
        {
            _offered.Add(options);
            return Task.FromResult<AddPlayerResult?>(new AddPlayerResult(PlayerPlatforms.Steam, id, name, role, true));
        };

    private static PlayerInfo Player(string id, string? name = null, PlayerStatus status = PlayerStatus.Offline) => new()
    {
        Platform = "Steam", PlatformRaw = "Steam", PlayerId = id, PlayerName = name, PlayerStatus = status,
    };

    private static string[] Keys(PlayerListSectionViewModel section) => section.Accounts.Select(r => r.Key).ToArray();

    // ---- data source ----

    [AvaloniaFact]
    public void Player_accounts_lists_every_known_player_except_default_banned()
    {
        _repo.PushUpdate(Player("1", "Nuffle"));  // no default role — still a known account
        _repo.PushUpdate(Player("2", "Mochi"));
        _repo.PushUpdate(Player("3", "Loki"));
        var vm = NewVm(("Steam:2", PlayerRole.Admin), ("Steam:3", PlayerRole.Banned));

        Assert.Equal(new[] { "Steam:2", "Steam:1" }, Keys(vm.PlayerAccounts)); // sorted by name
        Assert.Equal(new[] { "Steam:3" }, Keys(vm.Banned));
        Assert.Null(vm.PlayerAccounts.Accounts.First(r => r.Key == "Steam:1").RoleText);
        Assert.Equal(Strings.Role_Admin, vm.PlayerAccounts.Accounts.First(r => r.Key == "Steam:2").RoleText);
        Assert.False(vm.IsDirty);
    }

    [AvaloniaFact]
    public void A_default_without_a_cached_record_still_shows()
    {
        var vm = NewVm(("Steam:9", PlayerRole.Banned));

        var row = Assert.Single(vm.Banned.Accounts);
        Assert.False(row.HasAccountName);
        Assert.Equal("9", row.PlatformId);
    }

    [AvaloniaFact]
    public void Players_joining_while_open_appear()
    {
        var vm = NewVm();
        Assert.Empty(vm.PlayerAccounts.Accounts);

        _repo.PushUpdate(Player("1", "Nuffle", PlayerStatus.Online));

        Assert.Equal(new[] { "Steam:1" }, Keys(vm.PlayerAccounts));
    }

    // ---- add ----

    [AvaloniaFact]
    public async Task Each_tab_opens_Add_Player_with_its_preset()
    {
        var vm = NewVm();

        NextAdd(vm, "1", PlayerRole.Permitted);
        await vm.PlayerAccounts.AddCommand.ExecuteAsync(null);
        NextAdd(vm, "2", PlayerRole.Banned);
        await vm.Banned.AddCommand.ExecuteAsync(null);

        Assert.Equal(new[] { AddPlayerOptions.ForPlayerAccounts, AddPlayerOptions.ForBanned }, _offered);
    }

    [AvaloniaFact]
    public async Task Add_lands_by_role_selects_the_row_and_writes_nothing_until_save()
    {
        var vm = NewVm();

        NextAdd(vm, "1", PlayerRole.None, name: "Odin");
        await vm.PlayerAccounts.AddCommand.ExecuteAsync(null);
        NextAdd(vm, "2", PlayerRole.Banned, name: "Loki");
        await vm.PlayerAccounts.AddCommand.ExecuteAsync(null); // Banned from the accounts tab → Banned tab

        Assert.Equal(new[] { "Steam:1" }, Keys(vm.PlayerAccounts));
        Assert.Equal(new[] { "Steam:2" }, Keys(vm.Banned));
        Assert.Equal("Steam:2", vm.Banned.SelectedAccount?.Key);
        Assert.True(vm.IsDirty);
        Assert.Empty(_repo.Data);
        Assert.Empty(_prefs.LoadPreferences().PlayerDefaults);
    }

    [AvaloniaFact]
    public async Task Re_adding_an_existing_player_updates_role_and_name()
    {
        _repo.PushUpdate(Player("1", "Old"));
        var vm = NewVm(("Steam:1", PlayerRole.Banned));
        NextAdd(vm, "1", PlayerRole.Admin, name: "New");

        await vm.Banned.AddCommand.ExecuteAsync(null);

        Assert.Empty(vm.Banned.Accounts);
        var row = Assert.Single(vm.PlayerAccounts.Accounts);
        Assert.Equal("New", row.AccountName);
        Assert.Equal(Strings.Role_Admin, row.RoleText);
    }

    // ---- set default role ----

    [AvaloniaFact]
    public void Set_default_role_updates_in_place_and_None_clears_it()
    {
        _repo.PushUpdate(Player("1", "Odin"));
        var vm = NewVm();
        var section = vm.PlayerAccounts;
        section.SelectedAccount = section.Accounts[0];
        Assert.True(section.DefaultIsNone);

        section.DefaultIsAdmin = true;
        Assert.True(section.DefaultIsAdmin);
        Assert.Equal(Strings.Role_Admin, section.Accounts[0].RoleText);
        Assert.Same(section.Accounts[0], section.SelectedAccount);

        section.DefaultIsPermitted = false; // a radio group's uncheck write is ignored
        Assert.True(section.DefaultIsAdmin);

        section.DefaultIsNone = true;
        vm.Save();
        Assert.Empty(_prefs.LoadPreferences().PlayerDefaults);
    }

    [AvaloniaFact]
    public void Setting_Banned_moves_the_player_to_the_Banned_tab_and_back()
    {
        _repo.PushUpdate(Player("1", "Loki"));
        var vm = NewVm();
        vm.PlayerAccounts.SelectedAccount = vm.PlayerAccounts.Accounts[0];

        vm.PlayerAccounts.DefaultIsBanned = true;

        Assert.Empty(vm.PlayerAccounts.Accounts);
        Assert.Equal(new[] { "Steam:1" }, Keys(vm.Banned));

        vm.Banned.SelectedAccount = vm.Banned.Accounts[0];
        Assert.True(vm.Banned.DefaultIsBanned);
        vm.Banned.DefaultIsPermitted = true;

        Assert.Empty(vm.Banned.Accounts);
        Assert.Equal(Strings.Role_Permitted, Assert.Single(vm.PlayerAccounts.Accounts).RoleText);
    }

    // ---- remove ----

    [AvaloniaFact]
    public void Remove_forgets_the_player_and_their_default_on_save()
    {
        _repo.PushUpdate(Player("1", "Loki"));
        var vm = NewVm(("Steam:1", PlayerRole.Banned));
        vm.Banned.SelectedAccount = vm.Banned.Accounts[0];

        vm.Banned.RemoveCommand.Execute(null);

        Assert.Empty(vm.Banned.Accounts);
        Assert.NotNull(_repo.FindById("Steam:1")); // staged

        vm.Save();
        Assert.Null(_repo.FindById("Steam:1"));
        Assert.Empty(_prefs.LoadPreferences().PlayerDefaults);
    }

    [AvaloniaFact]
    public void Remove_needs_the_player_offline()
    {
        _repo.PushUpdate(Player("1", "Nuffle", PlayerStatus.Online));
        var vm = NewVm();
        vm.PlayerAccounts.SelectedAccount = vm.PlayerAccounts.Accounts[0];

        Assert.False(vm.PlayerAccounts.RemoveCommand.CanExecute(null));
    }

    // ---- known characters ----

    [AvaloniaFact]
    public void Known_characters_follow_the_selection_with_empty_states()
    {
        var withChars = Player("1", "Odin");
        withChars.Characters = new() { new() { CharacterName = "Ragnar", MatchConfident = true } };
        _repo.PushUpdate(withChars);
        _repo.PushUpdate(Player("2", "Thor"));
        var vm = NewVm();
        var kc = vm.PlayerAccounts.KnownCharacters;

        Assert.Equal(Strings.ManagePlayers_NoAccountSelected, kc.EmptyText);

        vm.PlayerAccounts.SelectedAccount = vm.PlayerAccounts.Accounts.First(r => r.Key == "Steam:1");
        Assert.Equal("Ragnar", Assert.Single(kc.Characters).CharacterName);

        vm.PlayerAccounts.SelectedAccount = vm.PlayerAccounts.Accounts.First(r => r.Key == "Steam:2");
        Assert.Equal(Strings.ManagePlayers_NoKnownCharacters, kc.EmptyText);
        Assert.False(vm.IsDirty);
    }

    [AvaloniaFact]
    public void The_banned_tab_has_its_own_known_characters()
    {
        var loki = Player("3", "Loki");
        loki.Characters = new() { new() { CharacterName = "Trickster", MatchConfident = true } };
        _repo.PushUpdate(loki);
        var vm = NewVm(("Steam:3", PlayerRole.Banned));

        vm.Banned.SelectedAccount = vm.Banned.Accounts[0];
        Assert.Equal("Trickster", Assert.Single(vm.Banned.KnownCharacters.Characters).CharacterName);
        Assert.Equal(Strings.ManagePlayers_NoAccountSelected, vm.PlayerAccounts.KnownCharacters.EmptyText);

        vm.Banned.KnownCharacters.AddCharacter("Shapeshifter");
        Assert.True(vm.IsDirty);
        vm.Save();
        Assert.Contains(_repo.FindById("Steam:3")!.Characters!, c => c.CharacterName == "Shapeshifter");
    }

    [AvaloniaFact]
    public void Selecting_a_character_does_not_mark_dirty()
    {
        var odin = Player("1", "Odin");
        odin.Characters = new() { new() { CharacterName = "Thor", MatchConfident = true } };
        _repo.PushUpdate(odin);
        var vm = NewVm();
        vm.PlayerAccounts.SelectedAccount = vm.PlayerAccounts.Accounts[0];

        vm.PlayerAccounts.KnownCharacters.SelectedCharacter = vm.PlayerAccounts.KnownCharacters.Characters[0];

        Assert.False(vm.IsDirty);
    }

    [AvaloniaFact]
    public void A_live_status_change_re_derives_character_status_and_keeps_unsaved_edits()
    {
        var online = Player("1", "Odin", PlayerStatus.Online);
        online.LastStatusCharacter = "Thor";
        online.Characters = new() { new() { CharacterName = "Thor", MatchConfident = true } };
        _repo.PushUpdate(online);
        var vm = NewVm();
        vm.PlayerAccounts.SelectedAccount = vm.PlayerAccounts.Accounts[0];
        var kc = vm.PlayerAccounts.KnownCharacters;
        kc.AddCharacter("Ragnar");
        var thor = kc.Characters.First(c => c.CharacterName == "Thor");
        Assert.Equal(PlayerStatus.Online, thor.Status);

        var offline = Player("1", "Odin", PlayerStatus.Offline);
        offline.LastStatusCharacter = "Thor";
        _repo.PushUpdate(offline);

        Assert.Equal(PlayerStatus.Offline, thor.Status);
        Assert.Contains(kc.Characters, c => c.CharacterName == "Ragnar");
    }

    [AvaloniaFact]
    public void Character_edits_are_staged_then_merged_onto_the_live_record_on_save()
    {
        _repo.PushUpdate(Player("1", "Odin", PlayerStatus.Online));
        var vm = NewVm();
        vm.PlayerAccounts.SelectedAccount = vm.PlayerAccounts.Accounts[0];

        vm.PlayerAccounts.KnownCharacters.AddCharacter("Ragnar");
        Assert.True(vm.IsDirty);
        Assert.Null(_repo.FindById("Steam:1")!.Characters);

        vm.Save();

        var saved = _repo.FindById("Steam:1")!;
        Assert.Contains(saved.Characters!, c => c.CharacterName == "Ragnar");
        Assert.Equal(PlayerStatus.Online, saved.PlayerStatus); // live fields kept
    }

    // ---- edit name ----

    // Makes the next Edit Player Name prompt return the given text (null = Cancel), recording the current name.
    private readonly List<string> _namePrompts = new();

    private void NextName(ManagePlayersViewModel vm, string? result)
        => vm.EditNamePrompt = current =>
        {
            _namePrompts.Add(current);
            return Task.FromResult(result);
        };

    [AvaloniaFact]
    public async Task Edit_name_is_staged_until_save()
    {
        _repo.PushUpdate(Player("1", "Odin"));
        var vm = NewVm();
        vm.PlayerAccounts.SelectedAccount = vm.PlayerAccounts.Accounts[0];
        NextName(vm, "  Allfather ");

        await vm.PlayerAccounts.EditNameCommand.ExecuteAsync(null);

        Assert.Equal(new[] { "Odin" }, _namePrompts);
        Assert.Equal("Allfather", vm.PlayerAccounts.Accounts[0].AccountName);
        Assert.True(vm.IsDirty);
        Assert.Equal("Odin", _repo.FindById("Steam:1")!.PlayerName);

        vm.Save();
        Assert.Equal("Allfather", _repo.FindById("Steam:1")!.PlayerName);
    }

    [AvaloniaFact]
    public async Task Edit_name_cancel_changes_nothing_and_blank_clears_the_name()
    {
        _repo.PushUpdate(Player("1", "Odin"));
        var vm = NewVm();
        vm.PlayerAccounts.SelectedAccount = vm.PlayerAccounts.Accounts[0];

        NextName(vm, null);
        await vm.PlayerAccounts.EditNameCommand.ExecuteAsync(null);
        Assert.Equal("Odin", vm.PlayerAccounts.Accounts[0].AccountName);
        Assert.False(vm.IsDirty);

        NextName(vm, "   ");
        await vm.PlayerAccounts.EditNameCommand.ExecuteAsync(null);
        Assert.False(vm.PlayerAccounts.Accounts[0].HasAccountName);
        vm.Save();
        Assert.Null(_repo.FindById("Steam:1")!.PlayerName);
    }

    [AvaloniaFact]
    public async Task Edit_name_keeps_staged_character_edits()
    {
        _repo.PushUpdate(Player("1", "Odin"));
        var vm = NewVm();
        vm.PlayerAccounts.SelectedAccount = vm.PlayerAccounts.Accounts[0];
        vm.PlayerAccounts.KnownCharacters.AddCharacter("Ragnar");
        NextName(vm, "Allfather");

        await vm.PlayerAccounts.EditNameCommand.ExecuteAsync(null);
        vm.Save();

        var saved = _repo.FindById("Steam:1")!;
        Assert.Equal("Allfather", saved.PlayerName);
        Assert.Contains(saved.Characters!, c => c.CharacterName == "Ragnar");
    }

    [AvaloniaFact]
    public void Edit_name_needs_a_selection()
    {
        _repo.PushUpdate(Player("1", "Odin"));
        var vm = NewVm();
        Assert.False(vm.PlayerAccounts.EditNameCommand.CanExecute(null));

        vm.PlayerAccounts.SelectedAccount = vm.PlayerAccounts.Accounts[0];
        Assert.True(vm.PlayerAccounts.EditNameCommand.CanExecute(null));
    }

    // ---- focus (View Player Details) ----

    [AvaloniaFact]
    public void Focus_player_opens_the_right_tab_and_selects_the_row_without_dirtying()
    {
        _repo.PushUpdate(Player("1", "Odin"));
        _repo.PushUpdate(Player("3", "Loki"));
        var vm = NewVm(("Steam:3", PlayerRole.Banned));

        vm.FocusPlayer("Steam:3");
        Assert.Equal(1, vm.SelectedTabIndex);
        Assert.Equal("Steam:3", vm.Banned.SelectedAccount?.Key);

        vm.FocusPlayer("Steam:1");
        Assert.Equal(0, vm.SelectedTabIndex);
        Assert.Equal("Steam:1", vm.PlayerAccounts.SelectedAccount?.Key);
        Assert.False(vm.IsDirty);
    }

    [AvaloniaFact]
    public void Focus_player_ignores_an_unknown_player()
    {
        _repo.PushUpdate(Player("1", "Odin"));
        var vm = NewVm();

        vm.FocusPlayer("Steam:404");

        Assert.Equal(0, vm.SelectedTabIndex);
        Assert.Null(vm.PlayerAccounts.SelectedAccount);
    }

    // ---- save / cancel / copy ----

    [AvaloniaFact]
    public async Task Save_writes_defaults_and_new_records_and_looks_up_unnamed_players()
    {
        var vm = NewVm();
        NextAdd(vm, "1", PlayerRole.Admin);                  // no name → lookup
        await vm.PlayerAccounts.AddCommand.ExecuteAsync(null);
        NextAdd(vm, "2", PlayerRole.Banned, name: "Loki");   // named → no lookup
        await vm.Banned.AddCommand.ExecuteAsync(null);

        vm.Save();

        var defaults = _prefs.LoadPreferences().PlayerDefaults;
        Assert.Equal(new PlayerDefaultEntry(PlayerRole.Admin, "Steam"), defaults["Steam:1"]);
        Assert.Equal(new PlayerDefaultEntry(PlayerRole.Banned, "Steam"), defaults["Steam:2"]);
        Assert.Equal("Loki", _repo.FindById("Steam:2")!.PlayerName);
        Assert.Equal(1, _api.RequestPlayerInfoCallCount);
    }

    [AvaloniaFact]
    public async Task Discarding_writes_nothing()
    {
        var vm = NewVm();
        var savesBefore = _prefs.SaveCount;
        NextAdd(vm, "1", PlayerRole.Admin, name: "Odin");

        await vm.PlayerAccounts.AddCommand.ExecuteAsync(null);
        vm.Dispose(); // the window closes without Save

        Assert.Equal(savesBefore, _prefs.SaveCount);
        Assert.Empty(_repo.Data);
    }

    [AvaloniaFact]
    public void Empty_tabs_show_the_add_hint()
    {
        var vm = NewVm();
        Assert.Equal(Strings.ManagePlayers_NoAccounts, vm.PlayerAccounts.AccountsEmptyText);
        Assert.Equal(Strings.ManagePlayers_NoAccounts, vm.Banned.AccountsEmptyText);
    }
}
