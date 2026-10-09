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
        var kc = vm.PlayerAccounts.KnownCharacters!;

        Assert.Equal("Select an account to see known characters.", kc.EmptyText);

        vm.PlayerAccounts.SelectedAccount = vm.PlayerAccounts.Accounts.First(r => r.Key == "Steam:1");
        Assert.Equal("Ragnar", Assert.Single(kc.Characters).CharacterName);

        vm.PlayerAccounts.SelectedAccount = vm.PlayerAccounts.Accounts.First(r => r.Key == "Steam:2");
        Assert.Equal("No known characters for this account.", kc.EmptyText);
        Assert.False(vm.IsDirty);
        Assert.Null(vm.Banned.KnownCharacters);
    }

    [AvaloniaFact]
    public void Character_edits_are_staged_then_merged_onto_the_live_record_on_save()
    {
        _repo.PushUpdate(Player("1", "Odin", PlayerStatus.Online));
        var vm = NewVm();
        vm.PlayerAccounts.SelectedAccount = vm.PlayerAccounts.Accounts[0];

        vm.PlayerAccounts.KnownCharacters!.AddCharacter("Ragnar");
        Assert.True(vm.IsDirty);
        Assert.Null(_repo.FindById("Steam:1")!.Characters);

        vm.Save();

        var saved = _repo.FindById("Steam:1")!;
        Assert.Contains(saved.Characters!, c => c.CharacterName == "Ragnar");
        Assert.Equal(PlayerStatus.Online, saved.PlayerStatus); // live fields kept
    }

    [AvaloniaFact]
    public void Player_details_opened_from_here_edits_the_staged_record()
    {
        _repo.PushUpdate(Player("1", "Odin"));
        var vm = NewVm();
        string? requested = null;
        vm.DetailsRequested += key => requested = key;
        vm.PlayerAccounts.SelectedAccount = vm.PlayerAccounts.Accounts[0];

        vm.PlayerAccounts.ViewDetailsCommand.Execute(null);
        Assert.Equal("Steam:1", requested);

        var details = new PlayerDetailsViewModel(_repo, "Steam:1", store: vm.Records) { DisplayName = "Allfather" };
        details.Save();
        vm.OnDetailsSaved("Steam:1");

        Assert.Equal("Allfather", vm.PlayerAccounts.Accounts[0].AccountName);
        Assert.Equal("Odin", _repo.FindById("Steam:1")!.PlayerName); // outer Save still authoritative

        vm.Save();
        Assert.Equal("Allfather", _repo.FindById("Steam:1")!.PlayerName);
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
        Assert.Equal(ManagePlayersViewModel.NoAccountsText, vm.PlayerAccounts.AccountsEmptyText);
        Assert.Equal(ManagePlayersViewModel.NoAccountsText, vm.Banned.AccountsEmptyText);
    }

    [Fact]
    public void Manage_players_copy_is_verbatim()
    {
        Assert.Equal("Add an account using the button below.", ManagePlayersViewModel.NoAccountsText);
        Assert.Equal("Select an account to see known characters.", ManagePlayersViewModel.NoAccountSelectedText);
        Assert.Equal("No known characters for this account.", ManagePlayersViewModel.NoKnownCharactersText);
    }
}
