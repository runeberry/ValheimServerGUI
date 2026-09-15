using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ValheimServerGUI.App.Tests.Fakes;
using ValheimServerGUI.App.ViewModels.Dialogs;
using ValheimServerGUI.Game;
using Xunit;

namespace ValheimServerGUI.App.Tests.ViewModels.Dialogs;

// §13.3: OK/Cancel/Restore-Defaults + unsaved-changes guard (dirty flag) + WorldPreferences preset↔custom.
public class DialogViewModelTests
{
    private static readonly System.IServiceProvider Core =
        new ServiceCollection().AddValheimCore().BuildServiceProvider();

    // ----- Preferences -----
    [Fact]
    public void Preferences_loads_clean_and_marks_dirty_on_edit()
    {
        var vm = new PreferencesViewModel(new FakeUserPreferencesProvider(), new FakeStartupManager());
        Assert.False(vm.IsDirty);
        vm.CheckForUpdates = !vm.CheckForUpdates;
        Assert.True(vm.IsDirty);
    }

    [Fact]
    public void Preferences_save_persists_and_applies_startup()
    {
        var prefs = new FakeUserPreferencesProvider();
        var startup = new FakeStartupManager();
        var vm = new PreferencesViewModel(prefs, startup) { StartWithWindows = true, Theme = AppTheme.Dark };

        vm.Save();

        Assert.True(prefs.LoadPreferences().StartWithWindows);
        Assert.Equal(AppTheme.Dark, prefs.LoadPreferences().Theme);
        Assert.Equal(new[] { true }, startup.Applied);
        Assert.Equal(AppTheme.Dark, vm.SavedTheme);
    }

    [Fact]
    public void Preferences_restore_defaults_resets_values()
    {
        var vm = new PreferencesViewModel(new FakeUserPreferencesProvider(), new FakeStartupManager())
        {
            CheckForUpdates = false,
            SaveProfileOnStart = false,
        };

        vm.ApplyDefaults();

        Assert.True(vm.CheckForUpdates); // default true
        Assert.True(vm.SaveProfileOnStart);
    }

    // ----- Directories -----
    [Fact]
    public void Directories_missing_path_is_detected()
    {
        var vm = new DirectoriesViewModel(new FakeUserPreferencesProvider(), Core.GetRequiredService<IValheimPathResolver>(), new RecordingShellLauncher())
        {
            ServerExePath = "/definitely/not/here",
        };
        Assert.True(vm.HasMissingPath);
    }

    [Fact]
    public void Directories_existing_paths_are_ok()
    {
        var dir = Directory.CreateTempSubdirectory().FullName;
        var vm = new DirectoriesViewModel(new FakeUserPreferencesProvider(), Core.GetRequiredService<IValheimPathResolver>(), new RecordingShellLauncher())
        {
            ServerExePath = string.Empty,
            SaveDataFolderPath = dir,
        };
        Assert.False(vm.HasMissingPath);
        Directory.Delete(dir);
    }

    // ----- WorldPreferences (preset ↔ custom, keys always persist) -----
    [Fact]
    public void World_editing_a_modifier_reverts_preset_to_custom()
    {
        var vm = new WorldPreferencesViewModel(new FakeWorldPreferencesProvider(), "W", new RecordingShellLauncher())
            { SelectedPreset = WorldGenPresets.Hard };
        Assert.True(vm.IsPresetSelected);

        vm.Modifiers[0].Selected = vm.Modifiers[0].Options[1]; // any non-Normal value

        Assert.Equal(WorldPreferencesViewModel.CustomPreset, vm.SelectedPreset);
        Assert.False(vm.IsPresetSelected);
    }

    [Fact]
    public void World_preset_persists_as_preset_only_but_keys_still_persist()
    {
        var provider = new FakeWorldPreferencesProvider();
        var vm = new WorldPreferencesViewModel(provider, "W", new RecordingShellLauncher())
            { SelectedPreset = WorldGenPresets.Hard };
        vm.Keys[0].IsSet = true; // a key
        Assert.Equal(WorldGenPresets.Hard, vm.SelectedPreset); // toggling a key does NOT revert the preset

        vm.Save();

        var saved = provider.LoadPreferences("W")!;
        Assert.Equal(WorldGenPresets.Hard, saved.Preset);
        Assert.Empty(saved.Modifiers);                 // modifiers not persisted under a preset
        Assert.Contains(WorldGenKeys.All[0], saved.Keys); // keys always persist (§15 #5)
    }

    [Fact]
    public void World_custom_persists_modifiers()
    {
        var provider = new FakeWorldPreferencesProvider();
        var vm = new WorldPreferencesViewModel(provider, "W", new RecordingShellLauncher());
        var combat = vm.Modifiers[0];
        combat.Selected = combat.Options[1]; // a friendly display name

        vm.Save();

        var saved = provider.LoadPreferences("W")!;
        Assert.Null(saved.Preset);
        // The dropdown shows friendly names but the raw token is what persists.
        Assert.Equal(combat.Value, saved.Modifiers[combat.Key]);
    }

    [Fact]
    public void World_loads_existing_preferences()
    {
        var provider = new FakeWorldPreferencesProvider();
        provider.SavePreferences(new WorldPreferences
        {
            WorldName = "W",
            Preset = WorldGenPresets.Casual,
            Keys = new() { WorldGenKeys.NoMap },
        });

        var vm = new WorldPreferencesViewModel(provider, "W", new RecordingShellLauncher());

        Assert.Equal(WorldGenPresets.Casual, vm.SelectedPreset);
        Assert.False(vm.IsDirty); // loaded clean
        Assert.Contains(vm.Keys, k => k.Key == WorldGenKeys.NoMap && k.IsSet);
    }

    [Fact]
    public void World_modifier_dropdowns_use_friendly_names_and_round_trip_tokens()
    {
        var provider = new FakeWorldPreferencesProvider();
        provider.SavePreferences(new WorldPreferences
        {
            WorldName = "W",
            Modifiers = new() { [WorldGenModifiers.Portals] = WorldGenModifiers.Values.PortalsVeryHard },
        });
        var vm = new WorldPreferencesViewModel(provider, "W", new RecordingShellLauncher());
        var portals = vm.Modifiers[4]; // Combat, DeathPenalty, Resources, Raids, Portals

        Assert.Equal(WorldGenModifiers.Portals, portals.Key);
        Assert.Equal("Portals", portals.DisplayName);
        Assert.Contains("Very Hard (No portals)", portals.Options);   // friendly names, not raw tokens
        Assert.Equal("Very Hard (No portals)", portals.Selected);     // loaded token -> friendly display
        Assert.Equal(WorldGenModifiers.Values.PortalsVeryHard, portals.Value); // and maps back to the token
    }

    [Fact]
    public void World_wiki_links_open_the_expected_urls()
    {
        var shell = new RecordingShellLauncher();
        var vm = new WorldPreferencesViewModel(new FakeWorldPreferencesProvider(), "W", shell);

        vm.OpenWorldModifiersWikiCommand.Execute(null);
        vm.OpenWorldModifiersHelpCommand.Execute(null);

        Assert.Equal(
            new[] { AppConstants.UrlValheimWikiWorldModifiers, AppConstants.UrlHelpWorldModifiers },
            shell.OpenedWebAddresses);
    }

    // ----- PlayerDetails -----
    [Fact]
    public void PlayerDetails_add_character_sets_dirty_and_saves_confident()
    {
        var repo = new FakePlayerDataRepository();
        repo.PushUpdate(new PlayerInfo { Platform = "Steam", PlayerId = "1", PlayerName = "Odin" });
        var vm = new PlayerDetailsViewModel(repo, "Steam:1");
        Assert.False(vm.IsDirty);

        vm.AddCharacter("Ragnar");
        Assert.True(vm.IsDirty);

        vm.Save();
        var player = repo.FindById("Steam:1")!;
        Assert.Contains(player.Characters!, c => c.CharacterName == "Ragnar" && c.MatchConfident);
    }

    [Fact]
    public void PlayerDetails_refresh_rederives_status_without_discarding_edits()
    {
        var repo = new FakePlayerDataRepository();
        repo.PushUpdate(new PlayerInfo
        {
            Platform = "Steam", PlayerId = "1", LastStatusCharacter = "Odin", PlayerStatus = PlayerStatus.Online,
            Characters = new System.Collections.Generic.List<PlayerInfo.CharacterInfo> { new() { CharacterName = "Odin" } },
        });
        var vm = new PlayerDetailsViewModel(repo, "Steam:1") { DisplayName = "Edited" };
        vm.AddCharacter("Ragnar");
        var odin = vm.Characters.First(c => c.CharacterName == "Odin");
        Assert.Equal(PlayerStatus.Online, odin.Status);  // the active character is online

        // The player goes offline while the dialog is open.
        repo.PushUpdate(new PlayerInfo
        {
            Platform = "Steam", PlayerId = "1", LastStatusCharacter = "Odin", PlayerStatus = PlayerStatus.Offline,
        });
        vm.RefreshCommand.Execute(null);

        Assert.Equal(PlayerStatus.Offline, odin.Status);            // status re-derived
        Assert.Equal("Edited", vm.DisplayName);                     // unsaved edit preserved
        Assert.Contains(vm.Characters, c => c.CharacterName == "Ragnar"); // unsaved edit preserved
    }

    [Fact]
    public void PlayerDetails_selecting_a_character_does_not_mark_dirty()
    {
        var repo = new FakePlayerDataRepository();
        repo.PushUpdate(new PlayerInfo
        {
            Platform = "Steam",
            PlayerId = "1",
            Characters = new System.Collections.Generic.List<PlayerInfo.CharacterInfo>
            {
                new() { CharacterName = "Odin", MatchConfident = true },
                new() { CharacterName = "Thor", MatchConfident = true },
            },
        });
        var vm = new PlayerDetailsViewModel(repo, "Steam:1");
        Assert.False(vm.IsDirty);

        // Selecting a name in the list is view state, not an edit (regression: this used to trip the guard).
        vm.SelectedCharacter = vm.Characters.First(c => c.CharacterName == "Thor");
        Assert.False(vm.IsDirty);

        // A real edit still marks dirty.
        vm.RemoveCharacterCommand.Execute(null);
        Assert.True(vm.IsDirty);
    }

    [Fact]
    public void PlayerDetails_display_name_shows_unknown_when_empty()
    {
        var repo = new FakePlayerDataRepository();
        repo.PushUpdate(new PlayerInfo { Platform = "Steam", PlayerId = "1" });
        var vm = new PlayerDetailsViewModel(repo, "Steam:1");

        Assert.Equal("(unknown)", vm.DisplayNameOrUnknown);

        vm.DisplayName = "Ragnar";
        Assert.Equal("Ragnar", vm.DisplayNameOrUnknown);

        vm.DisplayName = "   ";
        Assert.Equal("(unknown)", vm.DisplayNameOrUnknown);
    }

    [Fact]
    public void PlayerDetails_display_name_override_saves()
    {
        var repo = new FakePlayerDataRepository();
        repo.PushUpdate(new PlayerInfo { Platform = "Steam", PlayerId = "1" });
        var vm = new PlayerDetailsViewModel(repo, "Steam:1") { DisplayName = "Custom" };

        vm.Save();
        Assert.Equal("Custom", repo.FindById("Steam:1")!.PlayerName);
    }

    // ----- BugReport -----
    [Fact]
    public async Task BugReport_submits_with_bugreport_source()
    {
        var client = new FakeRuneberryApiClient();
        var vm = new BugReportViewModel(client) { Description = "It broke", ContactInfo = "me@example.com" };
        Assert.True(vm.CanSubmit);

        await vm.SubmitAsync();

        var report = Assert.Single(client.Reports);
        Assert.Equal("BugReport", report.Source);
        Assert.Equal("It broke", report.AdditionalInfo!["Description"]);
        Assert.Equal("me@example.com", report.AdditionalInfo!["ContactInfo"]);
    }

    [Fact]
    public void BugReport_cannot_submit_when_empty()
        => Assert.False(new BugReportViewModel(new FakeRuneberryApiClient()).CanSubmit);

    // ----- About -----
    [Fact]
    public void About_exposes_version_and_link_commands()
    {
        var shell = new ValheimServerGUI.App.Services.ShellLauncher(new Services.RecordingSystemShell(), TestLog.Silent);
        var vm = new AboutViewModel(shell);
        Assert.False(string.IsNullOrEmpty(vm.Version));
        Assert.True(vm.OpenGitHubCommand.CanExecute(null));
    }
}
