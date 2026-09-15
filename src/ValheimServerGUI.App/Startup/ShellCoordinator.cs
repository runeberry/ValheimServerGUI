using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using ValheimServerGUI.App.Infrastructure;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.App.Views;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools.Logging;

namespace ValheimServerGUI.App.Startup;

/// <summary>
/// The v2.4 <c>SplashForm.PrepareMainWindows</c> orchestration (§2.3), minus the platform lifetime glue
/// that <see cref="App"/> owns. Runs the async startup tasks, then creates one <see cref="MainWindow"/>
/// per selected profile over a per-window <see cref="MainWindowViewModel"/> (a factory so each gets its
/// own transient <see cref="ValheimServer"/>), loads the profile, and shows it (minimized if the user
/// preference says so). Also opens/focuses a window when a second launch forwards a profile name.
/// </summary>
internal sealed class ShellCoordinator
{
    private readonly IServerPreferencesProvider _serverPrefs;
    private readonly IUserPreferencesProvider _userPrefs;
    private readonly IStartupArgsProvider _startupArgs;
    private readonly Func<MainWindowViewModel> _viewModelFactory;
    private readonly WindowManager _windowManager;
    private readonly StartupService _startupService;
    private readonly IApplicationLogger _logger;

    public ShellCoordinator(
        IServerPreferencesProvider serverPrefs,
        IUserPreferencesProvider userPrefs,
        IStartupArgsProvider startupArgs,
        Func<MainWindowViewModel> viewModelFactory,
        WindowManager windowManager,
        StartupService startupService,
        IApplicationLogger logger)
    {
        _serverPrefs = serverPrefs;
        _userPrefs = userPrefs;
        _startupArgs = startupArgs;
        _viewModelFactory = viewModelFactory;
        _windowManager = windowManager;
        _startupService = startupService;
        _logger = logger;
    }

    public System.Threading.Tasks.Task RunStartupTasksAsync(IProgress<StartupProgress>? progress)
        => _startupService.RunAsync(progress);

    /// <summary>Creates, loads, and shows the startup windows (§2.3). Returns the windows in open order.</summary>
    public IReadOnlyList<MainWindow> CreateAndShowStartupWindows()
    {
        var userPrefs = _userPrefs.LoadPreferences();
        var profiles = _serverPrefs.LoadPreferences().ToList();
        var selection = ResolveSelection(profiles, userPrefs.LastActiveProfile);

        var plans = selection.CreateDefault
            ? new[] { new StartupWindowPlan(CreateDefaultProfile(), AutoStart: false) }
            : selection.Windows;

        var windows = new List<MainWindow>();
        foreach (var plan in plans)
            windows.Add(ShowWindowFor(plan, userPrefs.StartMinimized));

        return windows;
    }

    /// <summary>Opens a fresh window (File &gt; New Window) for the most-recently-saved profile, else Default.</summary>
    public void OpenNewWindow()
    {
        var profile = _serverPrefs.LoadPreferences().OrderByDescending(p => p.LastSaved).FirstOrDefault()
                      ?? CreateDefaultProfile();
        _logger.Information("Opening new window for profile '{profile}'", profile.ProfileName);
        ShowWindowFor(new StartupWindowPlan(profile, AutoStart: false), startMinimized: false);
    }

    /// <summary>Opens (or focuses) a window for a profile named by a forwarded second launch (§2.2).</summary>
    public void OpenOrFocusProfile(string? profileName)
    {
        var existing = _windowManager.Windows
            .FirstOrDefault(w => w.ViewModel?.CurrentProfile?.ProfileName == profileName);
        if (existing is not null)
        {
            existing.WindowState = WindowState.Normal;
            existing.Activate();
            return;
        }

        var profile = (profileName is not null ? _serverPrefs.LoadPreferences(profileName) : null)
                      ?? _serverPrefs.LoadPreferences().OrderByDescending(p => p.LastSaved).FirstOrDefault();
        if (profile is null) return;

        ShowWindowFor(new StartupWindowPlan(profile, AutoStart: false), startMinimized: false);
    }

    private MainWindow ShowWindowFor(StartupWindowPlan plan, bool startMinimized)
    {
        var viewModel = _viewModelFactory();
        viewModel.AutoStartOnLoad = plan.AutoStart;
        viewModel.LoadProfile(plan.Profile);

        var window = new MainWindow(viewModel);
        _windowManager.Register(window);
        window.Show();
        if (startMinimized)
            window.WindowState = WindowState.Minimized;

        // Auto-start of the server itself is wired to the real StartServer flow in Wave 4; the plan flag
        // (AutoStartOnLoad) is carried on the view-model for that.
        return window;
    }

    private StartupSelection ResolveSelection(IReadOnlyList<ServerPreferences> profiles, string? lastActive)
    {
        // A profile named on the command line (initial launch) opens just that one, if it exists.
        var named = _startupArgs.ServerProfileName;
        if (!string.IsNullOrWhiteSpace(named))
        {
            var match = profiles.FirstOrDefault(p => p.ProfileName == named);
            if (match is not null)
                return new StartupSelection(new[] { new StartupWindowPlan(match, match.AutoStart) }, CreateDefault: false);
        }

        return StartupProfileSelector.Select(profiles, lastActive);
    }

    private ServerPreferences CreateDefaultProfile()
    {
        var prefs = new ServerPreferences { ProfileName = CoreConstants.DefaultServerProfileName };
        _serverPrefs.SavePreferences(prefs); // stamps LastSaved
        return prefs;
    }
}
