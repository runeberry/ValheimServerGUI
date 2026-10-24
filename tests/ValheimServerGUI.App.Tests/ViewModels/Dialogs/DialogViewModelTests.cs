using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ValheimServerGUI.App.Controls;
using ValheimServerGUI.App.Tests.Fakes;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.App.ViewModels.Dialogs;
using ValheimServerGUI.Game;
using Xunit;
using ValheimServerGUI.Localization;

namespace ValheimServerGUI.App.Tests.ViewModels.Dialogs;

// Dialog view-models: OK/Cancel/Restore-Defaults + unsaved-changes guard (dirty flag) + WorldPreferences fields and derived preset.
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
    public void Preferences_save_persists_the_stored_preferences()
    {
        var prefs = new FakeUserPreferencesProvider();
        var vm = new PreferencesViewModel(prefs, new FakeStartupManager()) { Theme = AppTheme.Dark };

        vm.Save();

        Assert.Equal(AppTheme.Dark, prefs.LoadPreferences().Theme);
        Assert.Equal(AppTheme.Dark, vm.SavedTheme);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Preferences_start_with_os_loads_from_the_os_registration(bool enabled)
    {
        var vm = new PreferencesViewModel(new FakeUserPreferencesProvider(), new FakeStartupManager { Enabled = enabled });

        Assert.Equal(enabled, vm.StartWithOs);
        Assert.False(vm.IsDirty);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Preferences_save_without_toggling_start_with_os_never_touches_the_registration(bool enabled)
    {
        var startup = new FakeStartupManager { Enabled = enabled };
        var vm = new PreferencesViewModel(new FakeUserPreferencesProvider(), startup) { Theme = AppTheme.Dark };

        vm.Save();

        Assert.Empty(startup.Applied);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Preferences_toggling_start_with_os_applies_it_once(bool enabled)
    {
        var startup = new FakeStartupManager { Enabled = enabled };
        var vm = new PreferencesViewModel(new FakeUserPreferencesProvider(), startup);

        vm.StartWithOs = !enabled;
        vm.Save();
        vm.Save();

        Assert.Equal(new[] { !enabled }, startup.Applied);
    }

    [Fact]
    public void Preferences_toggling_start_with_os_back_before_saving_applies_nothing()
    {
        var startup = new FakeStartupManager { Enabled = true };
        var vm = new PreferencesViewModel(new FakeUserPreferencesProvider(), startup);

        vm.StartWithOs = false;
        vm.StartWithOs = true;
        vm.Save();

        Assert.Empty(startup.Applied);
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
    private static readonly IValheimPathResolver Paths = Core.GetRequiredService<IValheimPathResolver>();

    private static UserPreferences AppPaths() => new()
    {
        ServerExePath = "/srv/valheim/valheim_server.x86_64",
        SaveDataFolderPath = "/srv/saves",
        BepInExPreloaderPath = "/srv/valheim/BepInEx/core/BepInEx.Preloader.dll",
        DoorstopLibraryPath = "/srv/valheim/doorstop_libs/libdoorstop_x64.so",
    };

    private static DirectoriesViewModel Directories(FakeUserPreferencesProvider user, FakeServerPreferencesProvider servers,
        string? profileName = null, System.Func<string, bool>? isRunning = null)
        => new(user, servers, Paths, new RecordingShellLauncher(), profileName, isRunning);

    private static FakeServerPreferencesProvider Profiles(params ServerPreferences[] profiles) => new(profiles);

    [Fact]
    public void Directories_lists_all_profiles_a_separator_then_each_profile_by_name()
    {
        var vm = Directories(new FakeUserPreferencesProvider(AppPaths()),
            Profiles(new ServerPreferences { ProfileName = "zeta" }, new ServerPreferences { ProfileName = "Alpha" }));

        Assert.Equal(new object[] { DirectoryTarget.AllProfiles, DropdownSeparator.Instance, new DirectoryTarget("Alpha"), new DirectoryTarget("zeta") },
            vm.Targets);
        Assert.Equal(Strings.Directories_Target_AllProfiles, DirectoryTarget.AllProfiles.ToString());
        Assert.Equal(string.Format(Strings.Directories_Target_Profile, "Alpha"), new DirectoryTarget("Alpha").ToString());
    }

    [Fact]
    public void Directories_opens_on_all_profiles_showing_the_app_wide_paths()
    {
        var vm = Directories(new FakeUserPreferencesProvider(AppPaths()), Profiles(new ServerPreferences { ProfileName = "P" }));

        Assert.Equal(DirectoryTarget.AllProfiles, vm.SelectedTarget);
        Assert.Equal(AppPaths().ServerExePath, vm.ServerExePath);
        Assert.Equal(AppPaths().DoorstopLibraryPath, vm.DoorstopLibraryPath);
        Assert.Null(vm.Placeholder);
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public void Directories_opens_on_a_profile_showing_its_overrides_with_empty_ones_using_the_default()
    {
        var vm = Directories(new FakeUserPreferencesProvider(AppPaths()),
            Profiles(new ServerPreferences { ProfileName = "P", SaveDataFolderPath = "/srv/other-saves" }), profileName: "P");

        Assert.Equal(new DirectoryTarget("P"), vm.SelectedTarget);
        Assert.Equal(string.Empty, vm.ServerExePath);
        Assert.Equal("/srv/other-saves", vm.SaveDataFolderPath);
        Assert.Equal(Strings.Directories_UseDefault, vm.Placeholder);
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public void Directories_keeps_edits_across_switching_and_ok_saves_every_changed_target_only()
    {
        var user = new FakeUserPreferencesProvider(AppPaths());
        var servers = Profiles(new ServerPreferences { ProfileName = "A" }, new ServerPreferences { ProfileName = "B" });
        var vm = Directories(user, servers);

        vm.ServerExePath = "/srv/new/valheim_server.x86_64";
        vm.SelectedTarget = new DirectoryTarget("A");
        vm.BepInExPreloaderPath = "/mods/BepInEx/core/BepInEx.Preloader.dll";
        vm.SelectedTarget = new DirectoryTarget("B"); // visited, not changed
        vm.SelectedTarget = DirectoryTarget.AllProfiles;
        Assert.Equal("/srv/new/valheim_server.x86_64", vm.ServerExePath); // edit kept
        Assert.True(vm.IsDirty);

        vm.Save();

        Assert.Equal("/srv/new/valheim_server.x86_64", user.LoadPreferences().ServerExePath);
        Assert.Equal(AppPaths().SaveDataFolderPath, user.LoadPreferences().SaveDataFolderPath);
        var a = servers.LoadPreferences("A")!;
        Assert.Equal("/mods/BepInEx/core/BepInEx.Preloader.dll", a.BepInExPreloaderPath);
        Assert.Null(a.ServerExePath); // blank override stored as "none"
        Assert.Equal(System.DateTime.UnixEpoch, servers.LoadPreferences("B")!.LastSaved); // unchanged: not saved
    }

    [Fact]
    public void Directories_switching_targets_alone_is_not_an_edit()
    {
        var vm = Directories(new FakeUserPreferencesProvider(AppPaths()), Profiles(new ServerPreferences { ProfileName = "A" }));

        vm.SelectedTarget = new DirectoryTarget("A");
        vm.SelectedTarget = DirectoryTarget.AllProfiles;

        Assert.False(vm.IsDirty);
    }

    [Fact]
    public void Directories_restore_defaults_resets_app_paths_to_the_os_defaults_or_clears_a_profiles_overrides()
    {
        var vm = Directories(new FakeUserPreferencesProvider(AppPaths()),
            Profiles(new ServerPreferences { ProfileName = "A", ServerExePath = "/x", DoorstopLibraryPath = "/y" }));

        vm.ApplyDefaults();
        Assert.Equal(Paths.DefaultServerPath, vm.ServerExePath);
        Assert.Equal(Paths.DefaultSaveDataFolder, vm.SaveDataFolderPath);
        Assert.Equal(Paths.DefaultBepInExPreloaderPath, vm.BepInExPreloaderPath);
        Assert.Equal(Paths.DefaultDoorstopLibraryPath, vm.DoorstopLibraryPath);

        vm.SelectedTarget = new DirectoryTarget("A");
        vm.ApplyDefaults();
        Assert.All(new[] { vm.ServerExePath, vm.SaveDataFolderPath, vm.BepInExPreloaderPath, vm.DoorstopLibraryPath }, Assert.Empty);
    }

    [Fact]
    public void Directories_a_running_profile_is_read_only()
    {
        var vm = Directories(new FakeUserPreferencesProvider(AppPaths()),
            Profiles(new ServerPreferences { ProfileName = "Up" }, new ServerPreferences { ProfileName = "Down" }),
            isRunning: name => name == "Up");

        Assert.True(vm.IsInputEnabled); // all profiles
        vm.SelectedTarget = new DirectoryTarget("Up");
        Assert.False(vm.IsInputEnabled);
        vm.SelectedTarget = new DirectoryTarget("Down");
        Assert.True(vm.IsInputEnabled);
    }

    [Fact]
    public void Directories_a_changed_missing_server_binary_is_detected()
    {
        var vm = Directories(new FakeUserPreferencesProvider(AppPaths()), Profiles());

        vm.ServerExePath = "/definitely/not/here";

        Assert.Equal(string.Format(Strings.Directories_MissingServerExe, "/definitely/not/here"), vm.MissingPathDescription);
    }

    [Fact]
    public void Directories_existing_paths_are_ok()
    {
        var dir = Directory.CreateTempSubdirectory().FullName;
        var vm = Directories(new FakeUserPreferencesProvider(AppPaths()), Profiles(new ServerPreferences { ProfileName = "P" }), "P");

        vm.SaveDataFolderPath = dir; // the other overrides stay empty

        Assert.False(vm.HasMissingPath);
        Directory.Delete(dir);
    }

    // An unmodded server has no BepInEx files, so their (default) paths only count as missing once the user sets them.
    [Fact]
    public void Directories_missing_bepinex_paths_count_only_when_changed()
    {
        var dir = Directory.CreateTempSubdirectory().FullName;
        var app = AppPaths();
        app.ServerExePath = string.Empty;
        app.SaveDataFolderPath = dir;
        var vm = Directories(new FakeUserPreferencesProvider(app), Profiles());

        vm.SaveDataFolderPath = dir + "/"; // a change to the target, with the BepInEx defaults still missing
        Assert.False(vm.HasMissingPath);

        vm.DoorstopLibraryPath = "/mods/missing.so";
        if (vm.ShowsModPaths)
            Assert.Equal(string.Format(Strings.Directories_MissingDoorstopLibrary, "/mods/missing.so"), vm.MissingPathDescription);
        else
            Assert.False(vm.HasMissingPath);
        Directory.Delete(dir);
    }

    // ----- WorldPreferences (fields are the truth; the preset is derived from them) -----
    private static WorldPreferencesViewModel WorldVm(FakeWorldPreferencesProvider? provider = null)
        => new(provider ?? new FakeWorldPreferencesProvider(), "W", new RecordingShellLauncher());

    private static ModifierRow Row(WorldPreferencesViewModel vm, string key) => vm.Modifiers.Single(m => m.Key == key);

    private static WorldKeyToggle Toggle(WorldPreferencesViewModel vm, string key) => vm.Keys.Single(k => k.Key == key);

    [Fact]
    public void World_choosing_a_preset_sets_every_field_to_its_values()
    {
        var vm = WorldVm();
        Row(vm, WorldGenModifiers.Resources).SetSelectedQuiet(WorldGenModifiers.Values.ResourcesMost); // not in Hardcore
        Toggle(vm, WorldGenKeys.Fire).IsSet = true;                                                     // not in Hardcore

        vm.SelectedPresetDisplay = Strings.WorldGen_Preset_Hardcore;

        var hardcore = WorldGenPresets.Find(WorldGenPresets.Hardcore)!.Settings;
        Assert.Equal(hardcore.Modifiers, vm.CurrentSettings().Modifiers);
        Assert.Equal(hardcore.Keys, vm.CurrentSettings().Keys);
        Assert.Equal(Strings.WorldGen_Preset_Hardcore, vm.SelectedPresetDisplay);
        Assert.True(vm.IsDirty);
    }

    [Fact]
    public void World_editing_a_field_after_a_preset_shows_the_game_style_name()
    {
        var vm = WorldVm();
        vm.SelectedPresetDisplay = Strings.WorldGen_Preset_Hard;
        var changed = new List<string?>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        Row(vm, WorldGenModifiers.DeathPenalty).SetSelectedQuiet(WorldGenModifiers.Values.DeathPenaltyEasy);

        Assert.Null(vm.SelectedPresetDisplay);
        Assert.Equal(string.Format(Strings.WorldGen_Preset_Extras, Strings.WorldGen_Preset_Hard), vm.PresetPlaceholder);
        Assert.Contains(nameof(vm.SelectedPresetDisplay), changed);
        Assert.Contains(nameof(vm.PresetPlaceholder), changed);

        Row(vm, WorldGenModifiers.DeathPenalty).SetSelectedQuiet(null); // back to Normal
        Assert.Equal(Strings.WorldGen_Preset_Hard, vm.SelectedPresetDisplay);

        Toggle(vm, WorldGenKeys.NoMap).IsSet = true; // a key counts too
        Assert.Null(vm.SelectedPresetDisplay);
        Assert.Equal(string.Format(Strings.WorldGen_Preset_Extras, Strings.WorldGen_Preset_Hard), vm.PresetPlaceholder);
    }

    [Fact]
    public void World_fields_containing_no_preset_show_custom_which_is_never_an_option()
    {
        var vm = WorldVm();

        Row(vm, WorldGenModifiers.Combat).SetSelectedQuiet(WorldGenModifiers.Values.CombatHard);

        Assert.Null(vm.SelectedPresetDisplay);
        Assert.Equal(Strings.WorldGen_Preset_Custom, vm.PresetPlaceholder);
        Assert.DoesNotContain(Strings.WorldGen_Preset_Custom, vm.Presets);
    }

    [Fact]
    public void World_save_persists_the_fields()
    {
        var provider = new FakeWorldPreferencesProvider();
        var vm = WorldVm(provider);
        vm.SelectedPresetDisplay = Strings.WorldGen_Preset_Casual;

        vm.Save();

        var saved = provider.LoadPreferences("W")!;
        var casual = WorldGenPresets.Find(WorldGenPresets.Casual)!.Settings;
        Assert.Equal(casual.Modifiers, saved.Modifiers);
        Assert.Equal(casual.Keys, saved.Keys);
        Assert.Null(saved.ToFile().Preset);
    }

    [Fact]
    public void World_loads_fields_and_derives_the_preset()
    {
        var provider = new FakeWorldPreferencesProvider();
        provider.SavePreferences(new WorldPreferences
        {
            WorldName = "W",
            Modifiers = new(WorldGenPresets.Find(WorldGenPresets.Hard)!.Settings.Modifiers),
            Keys = new() { WorldGenKeys.NoMap },
        });

        var vm = WorldVm(provider);

        Assert.False(vm.IsDirty); // loaded clean
        Assert.True(Toggle(vm, WorldGenKeys.NoMap).IsSet);
        Assert.Null(vm.SelectedPresetDisplay);
        Assert.Equal(string.Format(Strings.WorldGen_Preset_Extras, Strings.WorldGen_Preset_Hard), vm.PresetPlaceholder);
    }

    [Fact]
    public void World_restore_defaults_returns_every_field_to_normal()
    {
        var vm = WorldVm();
        vm.SelectedPresetDisplay = Strings.WorldGen_Preset_Immersive;

        vm.ApplyDefaults();

        Assert.True(vm.CurrentSettings().IsDefault);
        Assert.Equal(Strings.WorldGen_Preset_Normal, vm.SelectedPresetDisplay);
        Assert.True(vm.IsDirty);
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
        Assert.Equal(Strings.WorldGen_Portals_Name, portals.DisplayName);
        Assert.Contains(Strings.WorldGen_Portals_VeryHard, portals.Options);   // friendly names, not raw tokens
        Assert.Equal(Strings.WorldGen_Portals_VeryHard, portals.Selected);     // loaded token -> friendly display
        Assert.Equal(WorldGenModifiers.Values.PortalsVeryHard, portals.Value); // and maps back to the token
    }

    [Fact]
    public void World_wiki_link_opens_the_expected_url()
    {
        var shell = new RecordingShellLauncher();
        var vm = new WorldPreferencesViewModel(new FakeWorldPreferencesProvider(), "W", shell);

        vm.OpenWorldModifiersWikiCommand.Execute(null);

        Assert.Equal(new[] { AppConstants.UrlValheimWikiWorldModifiers }, shell.OpenedWebAddresses);
    }

    // ----- KnownCharacters -----
    [Fact]
    public void KnownCharacters_empty_text_tracks_player_and_rows()
    {
        var vm = new KnownCharactersViewModel("Select an account.", "No characters.");
        Assert.Equal("Select an account.", vm.EmptyText);

        vm.Load(new PlayerInfo { Platform = "Steam", PlayerId = "1" });
        Assert.Equal("No characters.", vm.EmptyText);

        vm.AddCharacter("Ragnar");
        Assert.Null(vm.EmptyText);

        vm.Load(null);
        Assert.Equal("Select an account.", vm.EmptyText);
    }

    [Fact]
    public void KnownCharacters_edits_raise_Edited_and_need_a_player()
    {
        var vm = new KnownCharactersViewModel();
        var edits = 0;
        vm.Edited += (_, _) => edits++;

        vm.AddCharacter("Ignored"); // no player loaded
        Assert.Empty(vm.Characters);

        vm.Load(new PlayerInfo { Platform = "Steam", PlayerId = "1" });
        vm.AddCharacter("Ragnar");
        vm.RenameCharacter("Ragnar", "Bjorn");
        vm.SelectedCharacter = vm.Characters[0]; // selection is not an edit
        vm.RemoveCharacterCommand.Execute(null);

        Assert.Equal(3, edits);
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
        Assert.True(vm.OpenSourceCodeCommand.CanExecute(null));
    }

    [Fact]
    public void About_links_open_the_expected_urls()
    {
        var shell = new RecordingShellLauncher();
        var vm = new AboutViewModel(shell);

        vm.OpenSourceCodeCommand.Execute(null);
        vm.OpenDiscordCommand.Execute(null);
        vm.OpenTipJarCommand.Execute(null);
        vm.OpenValheimSiteCommand.Execute(null);

        Assert.Equal(
            new[] { AppConstants.UrlSourceCode, AppConstants.UrlDiscord, AppConstants.UrlTipJar, AppConstants.UrlValheimGameSite },
            shell.OpenedWebAddresses);
    }
}
