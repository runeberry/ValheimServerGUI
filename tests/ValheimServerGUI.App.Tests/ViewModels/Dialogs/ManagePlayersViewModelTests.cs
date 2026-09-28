using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using ValheimServerGUI.App.Tests.Fakes;
using ValheimServerGUI.App.ViewModels.Dialogs;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools.Models;
using Xunit;

namespace ValheimServerGUI.App.Tests.ViewModels.Dialogs;

public class ManagePlayersViewModelTests
{
    private readonly FakeUserPreferencesProvider _prefs = new();
    private readonly FakePlayerDataRepository _repo = new();
    private readonly FakeRuneberryApiClient _api = new();

    private readonly List<AddPlayerOptions> _offered = new();
    private readonly List<(string Title, string Body)> _messages = new();
    private readonly List<(string Title, string Body)> _questions = new();
    private bool _answerYes = true;

    private ManagePlayersViewModel NewVm(params (string key, PlayerCategory category, PlayerRole role)[] defaults)
    {
        if (defaults.Length > 0)
        {
            var prefs = new UserPreferences();
            foreach (var (key, category, role) in defaults)
                prefs.PlayerDefaults[key] = new PlayerDefaultEntry(category, role, "Steam");
            _prefs.SavePreferences(prefs);
        }

        var vm = new ManagePlayersViewModel(_prefs, _repo, _api)
        {
            MessagePrompt = (title, body) => { _messages.Add((title, body)); return Task.CompletedTask; },
            ChoicePrompt = (title, body) => { _questions.Add((title, body)); return Task.FromResult(_answerYes); },
        };
        return vm;
    }

    // Makes the next Add Player dialog return the given result, recording the options it was opened with.
    private void NextAdd(ManagePlayersViewModel vm, string id, PlayerRole role, string? name = null)
        => vm.AddPlayerPrompt = options =>
        {
            _offered.Add(options);
            return Task.FromResult<AddPlayerResult?>(new AddPlayerResult(PlayerPlatforms.Steam, id, name, role));
        };

    private static PlayerInfo Player(string id, string? name = null) => new()
    {
        Platform = "Steam", PlatformRaw = "Steam", PlayerId = id, PlayerName = name,
    };

    // ---- loading + add ----

    [AvaloniaFact]
    public void Loads_each_list_from_the_saved_defaults()
    {
        _repo.PushUpdate(Player("1", "Me"));
        var vm = NewVm(
            ("Steam:1", PlayerCategory.MyAccount, PlayerRole.Admin),
            ("Steam:2", PlayerCategory.Friend, PlayerRole.Permitted),
            ("Steam:3", PlayerCategory.Banned, PlayerRole.Banned));

        var mine = Assert.Single(vm.MyAccounts.Accounts);
        Assert.Equal("Me", mine.DisplayName);
        Assert.Equal("Admin", mine.RoleText);
        Assert.Equal("[…2]", Assert.Single(vm.Friends.Accounts).DisplayName); // no cached record → fallback
        Assert.Single(vm.Banned.Accounts);
        Assert.False(vm.IsDirty);
    }

    [AvaloniaFact]
    public async Task Each_tab_opens_Add_Player_with_its_preset()
    {
        var vm = NewVm();

        NextAdd(vm, "1", PlayerRole.Admin);
        await vm.MyAccounts.AddCommand.ExecuteAsync(null);
        NextAdd(vm, "2", PlayerRole.Permitted);
        await vm.Friends.AddCommand.ExecuteAsync(null);
        NextAdd(vm, "3", PlayerRole.Banned);
        await vm.Banned.AddCommand.ExecuteAsync(null);

        Assert.Equal(new[] { AddPlayerOptions.ForMyAccounts, AddPlayerOptions.ForFriends, AddPlayerOptions.ForBanned }, _offered);
    }

    [AvaloniaFact]
    public async Task Add_creates_a_selected_row_marks_dirty_and_writes_nothing_until_save()
    {
        var vm = NewVm();
        NextAdd(vm, "1", PlayerRole.Admin, name: "Odin");

        await vm.MyAccounts.AddCommand.ExecuteAsync(null);

        var row = Assert.Single(vm.MyAccounts.Accounts);
        Assert.Same(row, vm.MyAccounts.SelectedAccount);
        Assert.Equal("Odin", row.DisplayName);
        Assert.True(vm.IsDirty);
        Assert.Empty(_repo.Data);                                 // staged, not written
        Assert.Empty(_prefs.LoadPreferences().PlayerDefaults);    // staged, not written
    }

    [AvaloniaFact]
    public async Task Banned_add_always_stores_the_Banned_role()
    {
        var vm = NewVm();
        NextAdd(vm, "1", PlayerRole.Admin); // whatever the dialog says

        await vm.Banned.AddCommand.ExecuteAsync(null);
        vm.Save();

        Assert.Equal(PlayerRole.Banned, _prefs.LoadPreferences().PlayerDefaults["Steam:1"].DefaultRole);
    }

    [AvaloniaFact]
    public async Task Re_adding_to_the_same_list_updates_role_and_name_and_selects_it()
    {
        _repo.PushUpdate(Player("1", "Old"));
        var vm = NewVm(("Steam:1", PlayerCategory.Friend, PlayerRole.Permitted));
        NextAdd(vm, "1", PlayerRole.Admin, name: "New");

        await vm.Friends.AddCommand.ExecuteAsync(null);

        var row = Assert.Single(vm.Friends.Accounts);
        Assert.Same(row, vm.Friends.SelectedAccount);
        Assert.Equal("Admin", row.RoleText);
        Assert.Equal("New", row.DisplayName);
        Assert.Empty(_messages);
        Assert.Empty(_questions);
    }

    // ---- cross-list validation ----

    [AvaloniaFact]
    public async Task Adding_your_own_account_to_Friends_is_refused()
    {
        var vm = NewVm(("Steam:1", PlayerCategory.MyAccount, PlayerRole.Admin));
        NextAdd(vm, "1", PlayerRole.Permitted, name: "Me");

        await vm.Friends.AddCommand.ExecuteAsync(null);

        Assert.Equal(("Add Player", "Player Me is already set as your own account. You cannot add yourself to the Friends list."),
            Assert.Single(_messages));
        Assert.Empty(vm.Friends.Accounts);
        Assert.Single(vm.MyAccounts.Accounts);
        Assert.False(vm.IsDirty);
    }

    [AvaloniaFact]
    public async Task Adding_your_own_account_to_Banned_is_refused()
    {
        _repo.PushUpdate(Player("1", "Me"));
        var vm = NewVm(("Steam:1", PlayerCategory.MyAccount, PlayerRole.Admin));
        NextAdd(vm, "1", PlayerRole.Banned);

        await vm.Banned.AddCommand.ExecuteAsync(null);

        // With no name entered, the cached name is used.
        Assert.Equal("Player Me is already set as your own account. You cannot add yourself to the Banned list.",
            Assert.Single(_messages).Body);
        Assert.Empty(vm.Banned.Accounts);
    }

    [AvaloniaFact]
    public async Task Claiming_a_friend_as_your_account_moves_them_on_Yes()
    {
        var vm = NewVm(("Steam:1", PlayerCategory.Friend, PlayerRole.Permitted));
        NextAdd(vm, "1", PlayerRole.Admin);

        await vm.MyAccounts.AddCommand.ExecuteAsync(null);

        Assert.Equal(("Add Player", "Player […1] is already on the Friends list. Claim this as your account instead?"),
            Assert.Single(_questions));
        Assert.Empty(vm.Friends.Accounts);
        Assert.Equal("Admin", Assert.Single(vm.MyAccounts.Accounts).RoleText); // moved with the dialog's role
    }

    [AvaloniaFact]
    public async Task Claiming_a_banned_player_as_your_account_is_left_alone_on_No()
    {
        var vm = NewVm(("Steam:1", PlayerCategory.Banned, PlayerRole.Banned));
        _answerYes = false;
        NextAdd(vm, "1", PlayerRole.Admin, name: "Loki");

        await vm.MyAccounts.AddCommand.ExecuteAsync(null);

        Assert.Equal("Player Loki is already on the Banned list. Claim this as your account instead?",
            Assert.Single(_questions).Body);
        Assert.Single(vm.Banned.Accounts);
        Assert.Empty(vm.MyAccounts.Accounts);
        Assert.False(vm.IsDirty);
    }

    [AvaloniaFact]
    public async Task Banning_a_friend_moves_them_on_Yes_with_the_Banned_role()
    {
        var vm = NewVm(("Steam:1", PlayerCategory.Friend, PlayerRole.Admin));
        NextAdd(vm, "1", PlayerRole.Banned, name: "Loki");

        await vm.Banned.AddCommand.ExecuteAsync(null);
        vm.Save();

        Assert.Equal("Player Loki is already on the Friends list. Move them to the Banned list instead?",
            Assert.Single(_questions).Body);
        Assert.Empty(vm.Friends.Accounts);
        var entry = _prefs.LoadPreferences().PlayerDefaults["Steam:1"];
        Assert.Equal(new PlayerDefaultEntry(PlayerCategory.Banned, PlayerRole.Banned, "Steam"), entry);
    }

    [AvaloniaFact]
    public async Task Befriending_a_banned_player_is_left_alone_on_No()
    {
        var vm = NewVm(("Steam:1", PlayerCategory.Banned, PlayerRole.Banned));
        _answerYes = false;
        NextAdd(vm, "1", PlayerRole.Permitted, name: "Loki");

        await vm.Friends.AddCommand.ExecuteAsync(null);

        Assert.Equal("Player Loki is already on the Banned list. Move them to the Friends list instead?",
            Assert.Single(_questions).Body);
        Assert.Single(vm.Banned.Accounts);
        Assert.Empty(vm.Friends.Accounts);
    }

    // ---- editing ----

    [AvaloniaFact]
    public void Role_verbs_set_and_clear_the_default_role()
    {
        var vm = NewVm(("Steam:1", PlayerCategory.Friend, PlayerRole.Permitted));
        vm.Friends.SelectedAccount = vm.Friends.Accounts[0];
        Assert.Equal("Revoke join permission", vm.Friends.PermitToggleLabel);
        Assert.False(vm.IsDirty); // selection is view state

        vm.Friends.ToggleAdminCommand.Execute(null);
        Assert.Equal("Admin", vm.Friends.Accounts[0].RoleText);
        Assert.Equal("Remove admin", vm.Friends.AdminToggleLabel);
        Assert.True(vm.IsDirty);

        vm.Friends.ToggleAdminCommand.Execute(null);
        vm.Save();
        Assert.Equal(PlayerRole.None, _prefs.LoadPreferences().PlayerDefaults["Steam:1"].DefaultRole);
    }

    [AvaloniaFact]
    public void Banned_list_has_no_role_verbs_or_known_characters()
    {
        var vm = NewVm(("Steam:1", PlayerCategory.Banned, PlayerRole.Banned));
        vm.Banned.SelectedAccount = vm.Banned.Accounts[0];

        Assert.False(vm.Banned.HasRoleCommands);
        Assert.False(vm.Banned.ToggleAdminCommand.CanExecute(null));
        Assert.Null(vm.Banned.KnownCharacters);
    }

    [AvaloniaFact]
    public void Remove_drops_list_membership_but_keeps_the_player_record()
    {
        _repo.PushUpdate(Player("1", "Odin"));
        var vm = NewVm(("Steam:1", PlayerCategory.Friend, PlayerRole.Permitted));
        vm.Friends.SelectedAccount = vm.Friends.Accounts[0];

        vm.Friends.RemoveCommand.Execute(null);
        vm.Save();

        Assert.Empty(vm.Friends.Accounts);
        Assert.Empty(_prefs.LoadPreferences().PlayerDefaults);
        Assert.NotNull(_repo.FindById("Steam:1"));
    }

    [AvaloniaFact]
    public void Known_characters_follow_the_selection_with_empty_states()
    {
        var withChars = Player("1", "Odin");
        withChars.Characters = new() { new() { CharacterName = "Ragnar", MatchConfident = true } };
        _repo.PushUpdate(withChars);
        var vm = NewVm(
            ("Steam:1", PlayerCategory.Friend, PlayerRole.Permitted),
            ("Steam:2", PlayerCategory.Friend, PlayerRole.Permitted));
        var kc = vm.Friends.KnownCharacters!;

        Assert.Equal("Select an account to see Known Characters.", kc.EmptyText);

        vm.Friends.SelectedAccount = vm.Friends.Accounts.First(r => r.Key == "Steam:1");
        Assert.Equal("Ragnar", Assert.Single(kc.Characters).CharacterName);
        Assert.Null(kc.EmptyText);

        vm.Friends.SelectedAccount = vm.Friends.Accounts.First(r => r.Key == "Steam:2");
        Assert.Empty(kc.Characters);
        Assert.Equal("No known characters for this account.", kc.EmptyText);
        Assert.False(vm.IsDirty);
    }

    [AvaloniaFact]
    public void Character_edits_are_staged_then_merged_onto_the_live_record_on_save()
    {
        var live = Player("1", "Odin");
        live.PlayerStatus = PlayerStatus.Online;
        _repo.PushUpdate(live);
        var vm = NewVm(("Steam:1", PlayerCategory.MyAccount, PlayerRole.Admin));
        vm.MyAccounts.SelectedAccount = vm.MyAccounts.Accounts[0];

        vm.MyAccounts.KnownCharacters!.AddCharacter("Ragnar");

        Assert.True(vm.IsDirty);
        Assert.Null(_repo.FindById("Steam:1")!.Characters); // not written yet

        vm.Save();

        var saved = _repo.FindById("Steam:1")!;
        Assert.Contains(saved.Characters!, c => c.CharacterName == "Ragnar");
        Assert.Equal(PlayerStatus.Online, saved.PlayerStatus); // live fields kept
    }

    [AvaloniaFact]
    public void Characters_added_to_an_account_with_no_cached_record_create_it_on_save()
    {
        var vm = NewVm(("Steam:9", PlayerCategory.Friend, PlayerRole.Permitted)); // listed, never joined
        vm.Friends.SelectedAccount = vm.Friends.Accounts[0];

        vm.Friends.KnownCharacters!.AddCharacter("Ragnar");
        vm.Save();

        Assert.Contains(_repo.FindById("Steam:9")!.Characters!, c => c.CharacterName == "Ragnar");
    }

    [AvaloniaFact]
    public void Player_details_opened_from_here_edits_the_staged_record()
    {
        _repo.PushUpdate(Player("1", "Odin"));
        var vm = NewVm(("Steam:1", PlayerCategory.Friend, PlayerRole.Permitted));
        string? requested = null;
        vm.DetailsRequested += key => requested = key;
        vm.Friends.SelectedAccount = vm.Friends.Accounts[0];

        vm.Friends.ViewDetailsCommand.Execute(null);
        Assert.Equal("Steam:1", requested);

        // What the window does: Player Details over the dialog's staged records, then report the save back.
        var details = new PlayerDetailsViewModel(_repo, "Steam:1", store: vm.Records) { DisplayName = "Allfather" };
        details.Save();
        vm.OnDetailsSaved("Steam:1");

        Assert.Equal("Allfather", vm.Friends.Accounts[0].DisplayName);
        Assert.True(vm.IsDirty);
        Assert.Equal("Odin", _repo.FindById("Steam:1")!.PlayerName); // outer Save still authoritative

        vm.Save();
        Assert.Equal("Allfather", _repo.FindById("Steam:1")!.PlayerName);
    }

    // ---- save / cancel ----

    [AvaloniaFact]
    public async Task Save_writes_defaults_and_new_records_and_looks_up_unnamed_players()
    {
        var vm = NewVm();
        NextAdd(vm, "1", PlayerRole.Admin);                 // no name → lookup
        await vm.MyAccounts.AddCommand.ExecuteAsync(null);
        NextAdd(vm, "2", PlayerRole.Permitted, name: "Thor"); // named → no lookup
        await vm.Friends.AddCommand.ExecuteAsync(null);

        vm.Save();

        var defaults = _prefs.LoadPreferences().PlayerDefaults;
        Assert.Equal(new PlayerDefaultEntry(PlayerCategory.MyAccount, PlayerRole.Admin, "Steam"), defaults["Steam:1"]);
        Assert.Equal(new PlayerDefaultEntry(PlayerCategory.Friend, PlayerRole.Permitted, "Steam"), defaults["Steam:2"]);
        Assert.NotNull(_repo.FindById("Steam:1"));
        Assert.Equal("Thor", _repo.FindById("Steam:2")!.PlayerName);
        Assert.Equal(1, _api.RequestPlayerInfoCallCount);
    }

    [AvaloniaFact]
    public async Task Discarding_writes_nothing()
    {
        var vm = NewVm();
        var savesBefore = _prefs.SaveCount;
        NextAdd(vm, "1", PlayerRole.Admin, name: "Odin");

        await vm.MyAccounts.AddCommand.ExecuteAsync(null);
        vm.Dispose(); // the window closes without Save

        Assert.Equal(savesBefore, _prefs.SaveCount);
        Assert.Empty(_repo.Data);
    }

    // ---- exact copy ----

    [Fact]
    public void Manage_players_copy_is_verbatim()
    {
        Assert.Equal("Add Player", ManagePlayersViewModel.AddPlayerTitle);
        Assert.Equal("Player {name} is already set as your own account. You cannot add yourself to the {list} list.",
            ManagePlayersViewModel.SelfOnListMessage);
        Assert.Equal("Player {name} is already on the {list} list. Claim this as your account instead?",
            ManagePlayersViewModel.ClaimAccountMessage);
        Assert.Equal("Player {name} is already on the {list} list. Move them to the {target} list instead?",
            ManagePlayersViewModel.MoveListMessage);
        Assert.Equal("Add your account here and set default permissions for any server you host.",
            ManagePlayersViewModel.MyAccountsCaption);
        Assert.Equal("Add your friends' accounts here and set default permissions for any server you host.",
            ManagePlayersViewModel.FriendsCaption);
        Assert.Equal("Select an account to see Known Characters.", ManagePlayersViewModel.NoAccountSelectedText);
        Assert.Equal("No known characters for this account.", ManagePlayersViewModel.NoKnownCharactersText);
    }
}
