using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using ValheimServerGUI.App.Services;
using ValheimServerGUI.App.Tests.Fakes;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.Game;
using Xunit;

namespace ValheimServerGUI.App.Tests.ViewModels;

/// <summary>
/// The launch settings that live outside the main form: directory overrides (owned by the Set Directories dialog)
/// fall back to the app-wide paths and survive saving the form; the environment variables travel with the form.
/// </summary>
public class ProfileDirectoriesTests
{
    private static readonly IServiceProvider Core =
        new ServiceCollection().AddValheimCore().BuildServiceProvider();

    private static readonly UserPreferences AppPaths = new()
    {
        ServerExePath = "/srv/valheim/valheim_server.x86_64",
        SaveDataFolderPath = "/srv/saves",
        BepInExPreloaderPath = "/srv/valheim/BepInEx/core/BepInEx.Preloader.dll",
        DoorstopLibraryPath = "/srv/valheim/doorstop_libs/libdoorstop_x64.so",
    };

    private static MainWindowViewModel Build(ServerPreferences profile, out FakeServerPreferencesProvider serverPrefs)
    {
        serverPrefs = new FakeServerPreferencesProvider(new[] { profile });
        var vm = new MainWindowViewModel(
            new ServerManager(() => Core.GetRequiredService<ValheimServer>(), Core.GetRequiredService<Serilog.ILogger>()),
            new FakeUserPreferencesProvider(AppPaths),
            serverPrefs,
            Core.GetRequiredService<IWorldPreferencesProvider>(),
            new FakeSteamCloudWorldProvider(),
            new FakeIpAddressProvider(),
            Core.GetRequiredService<IPlayerDataRepository>(),
            Core.GetRequiredService<ValheimServerGUI.Tools.Logging.IApplicationLogger>(),
            new FakeSoftwareUpdateProvider(),
            new ShellLauncher(new Services.RecordingSystemShell(), TestLog.Silent, isWindows: false),
            Core.GetRequiredService<IValheimPathResolver>(),
            Core.GetRequiredService<IPlayerListImportService>());
        vm.LoadProfile(profile);
        return vm;
    }

    [Fact]
    public void Options_use_each_profile_override_else_the_app_wide_path()
    {
        var vm = Build(new ServerPreferences
        {
            ProfileName = "P",
            SaveDataFolderPath = "/srv/other-saves",
            DoorstopLibraryPath = "/mods/doorstop_libs/libdoorstop_x64.so",
        }, out _);

        var options = vm.BuildOptions();

        Assert.Equal(AppPaths.ServerExePath, options.ServerExePath);
        Assert.Equal("/srv/other-saves", options.SaveDataFolderPath);
        Assert.Equal(AppPaths.BepInExPreloaderPath, options.BepInExPreloaderPath);
        Assert.Equal("/mods/doorstop_libs/libdoorstop_x64.so", options.DoorstopLibraryPath);
    }

    [Fact]
    public void Saving_the_form_keeps_directory_overrides_set_in_the_dialog()
    {
        var vm = Build(new ServerPreferences { ProfileName = "P" }, out var serverPrefs);

        // The dialog saves an override while the form has unsaved edits of its own.
        vm.Form.Name = "Edited";
        var stored = serverPrefs.LoadPreferences("P")!;
        stored.ServerExePath = "/srv/modded/valheim_server.x86_64";
        stored.BepInExPreloaderPath = "/srv/modded/BepInEx/core/BepInEx.Preloader.dll";
        serverPrefs.SavePreferences(stored);

        serverPrefs.SavePreferences(vm.BuildPreferences());

        var saved = serverPrefs.LoadPreferences("P")!;
        Assert.Equal("Edited", saved.Name);
        Assert.Equal("/srv/modded/valheim_server.x86_64", saved.ServerExePath);
        Assert.Equal("/srv/modded/BepInEx/core/BepInEx.Preloader.dll", saved.BepInExPreloaderPath);
    }

    [Fact]
    public void Environment_variables_travel_from_the_profile_through_the_form_to_the_launch()
    {
        var vm = Build(new ServerPreferences { ProfileName = "P", EnvironmentVariables = "A=1" }, out _);
        Assert.Equal("A=1", vm.Form.EnvironmentVariables);

        vm.Form.EnvironmentVariables = @"A=1 B=""two words""";

        Assert.Equal(@"A=1 B=""two words""", vm.BuildPreferences().EnvironmentVariables);
        Assert.Equal(@"A=1 B=""two words""", vm.BuildOptions().EnvironmentVariables);
    }

    [Fact]
    public void Set_directories_on_the_advanced_tab_asks_for_the_profiles_directories()
    {
        var vm = Build(new ServerPreferences { ProfileName = "P" }, out _);
        var requested = new List<MenuAction>();
        vm.MenuActionRequested += requested.Add;

        vm.SetProfileDirectoriesCommand.Execute(null);
        vm.SetDirectoriesCommand.Execute(null);

        Assert.Equal(new[] { MenuAction.SetProfileDirectories, MenuAction.SetDirectories }, requested);
    }
}
