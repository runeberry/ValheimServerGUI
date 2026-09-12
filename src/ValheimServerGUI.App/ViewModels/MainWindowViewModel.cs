using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.App.ViewModels;

/// <summary>A dialog-opening menu action the window's code-behind routes to the right (Wave 6) dialog.</summary>
public enum MenuAction
{
    NewProfile,
    SaveProfile,
    SaveProfileAs,
    RemoveProfile,
    Preferences,
    SetDirectories,
    BugReport,
    About,
}

/// <summary>
/// One server window's view-model (§10.5). Owns a transient <see cref="ValheimServer"/> over the shared
/// singleton providers. Exposes the chrome surface: server status + the single <c>Can*</c>/
/// <c>AllowServerChanges</c> gate (buttons AND tray derive from these — never duplicated), the update-check
/// status, the profile list behind the File menu, and commands for the menus/status bar. Window/lifetime
/// actions (New Window, Close, Start flow, dialog opens) are surfaced as events the code-behind wires, so
/// the view-model stays free of window/dialog references.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ValheimServer _server;
    private readonly IUserPreferencesProvider _userPrefs;
    private readonly IServerPreferencesProvider _serverPrefs;
    private readonly ISoftwareUpdateProvider _updateProvider;
    private readonly IShellLauncher _shell;
    private readonly IValheimPathResolver _pathResolver;

    private string? _updateLinkTarget;

    public MainWindowViewModel(
        ValheimServer server,
        IUserPreferencesProvider userPrefs,
        IServerPreferencesProvider serverPrefs,
        ISoftwareUpdateProvider updateProvider,
        IShellLauncher shell,
        IValheimPathResolver pathResolver)
    {
        _server = server;
        _userPrefs = userPrefs;
        _serverPrefs = serverPrefs;
        _updateProvider = updateProvider;
        _shell = shell;
        _pathResolver = pathResolver;

        _serverStatus = _server.Status;

        _server.StatusChanged += OnServerStatusChanged;
        _updateProvider.UpdateCheckStarted += OnUpdateCheckStarted;
        _updateProvider.UpdateCheckFinished += OnUpdateCheckFinished;
        _serverPrefs.PreferencesSaved += OnServerPreferencesSaved;

        RefreshProfiles();
    }

    // --- events the code-behind routes (keeps the VM free of window/dialog references) ---
    public event Action? NewWindowRequested;
    public event Action? CloseRequested;
    public event Action<bool>? StartServerRequested;      // isManual — the full flow lands in Wave 4
    public event Action<MenuAction>? MenuActionRequested;  // dialog opens — Wave 6
    public event Action<string>? RemoveProfileRequested;   // profile name — Wave 6 (confirm + remove)

    public ValheimServer Server => _server;

    public bool AutoStartOnLoad { get; set; }

    // --- profile identity (single CurrentProfile source: title, tray tooltip, tray header all read it) ---
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    private ServerPreferences? _currentProfile;

    public string Title => CurrentProfile is null
        ? AppConstants.ProductName
        : $"{AppConstants.ProductName} — {CurrentProfile.ProfileName}";

    /// <summary>Names of all saved profiles (behind Load/Remove submenus). Kept in sync with saves.</summary>
    public ObservableCollection<string> Profiles { get; } = new();

    public bool HasProfiles => Profiles.Count > 0;

    // --- server status + the single Can*/AllowServerChanges gate ---
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanStart), nameof(CanStop), nameof(CanRestart), nameof(AllowServerChanges), nameof(StatusText))]
    [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(StopCommand), nameof(RestartCommand), nameof(NewProfileCommand), nameof(LoadProfileCommand))]
    private ServerStatus _serverStatus;

    // Buttons AND tray items bind these (via the commands), and they derive from the one mirrored
    // ServerStatus — the same shape as ValheimServer.Can*, but a single source so nothing drifts.
    public bool CanStart => ServerStatus == ServerStatus.Stopped;
    public bool CanStop => ServerStatus is ServerStatus.Starting or ServerStatus.Running;
    public bool CanRestart => ServerStatus == ServerStatus.Running;

    /// <summary>The single "server changes allowed" gate: fields are editable only while Stopped (§10.2).</summary>
    public bool AllowServerChanges => ServerStatus == ServerStatus.Stopped;

    public string StatusText => ServerStatus.ToString();

    // --- update-check status (status-bar right; link when actionable) ---
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UpdateLinkCommand))]
    private string _updateStatusText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UpdateLinkCommand))]
    private bool _updateIsLink;

    // ===== commands =====

    [RelayCommand(CanExecute = nameof(CanStart))]
    private void Start() => StartServerRequested?.Invoke(true);

    [RelayCommand(CanExecute = nameof(CanStop))]
    private void Stop() => _server.Stop();

    [RelayCommand(CanExecute = nameof(CanRestart))]
    private void Restart() => _server.Restart();

    [RelayCommand]
    private void NewWindow() => NewWindowRequested?.Invoke();

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();

    [RelayCommand(CanExecute = nameof(AllowServerChanges))]
    private void NewProfile() => MenuActionRequested?.Invoke(MenuAction.NewProfile);

    [RelayCommand]
    private void SaveProfile() => MenuActionRequested?.Invoke(MenuAction.SaveProfile);

    [RelayCommand]
    private void SaveProfileAs() => MenuActionRequested?.Invoke(MenuAction.SaveProfileAs);

    [RelayCommand(CanExecute = nameof(AllowServerChanges))]
    private void LoadProfile(string profileName)
    {
        var profile = _serverPrefs.LoadPreferences(profileName);
        if (profile is not null) LoadProfile(profile);
    }

    [RelayCommand]
    private void RemoveProfile(string profileName) => RemoveProfileRequested?.Invoke(profileName);

    [RelayCommand]
    private void Preferences() => MenuActionRequested?.Invoke(MenuAction.Preferences);

    [RelayCommand]
    private void SetDirectories() => MenuActionRequested?.Invoke(MenuAction.SetDirectories);

    [RelayCommand]
    private void OpenSettingsDirectory()
    {
        var dir = System.IO.Path.GetDirectoryName(_pathResolver.UserPrefsFilePath);
        if (!string.IsNullOrEmpty(dir)) _shell.OpenDirectory(dir);
    }

    [RelayCommand]
    private void OnlineManual() => _shell.OpenWebAddress(AppConstants.UrlHelp);

    [RelayCommand]
    private void PortForwarding() => _shell.OpenWebAddress(AppConstants.UrlPortForwarding);

    [RelayCommand]
    private void BugReport() => MenuActionRequested?.Invoke(MenuAction.BugReport);

    [RelayCommand]
    private async System.Threading.Tasks.Task CheckForUpdates()
        => await _updateProvider.CheckForUpdatesAsync(isManualCheck: true);

    [RelayCommand]
    private void Discord() => _shell.OpenWebAddress(AppConstants.UrlDiscord);

    [RelayCommand]
    private void About() => MenuActionRequested?.Invoke(MenuAction.About);

    [RelayCommand(CanExecute = nameof(UpdateIsLink))]
    private void UpdateLink()
    {
        if (!string.IsNullOrEmpty(_updateLinkTarget))
            _shell.OpenWebAddress(_updateLinkTarget);
    }

    /// <summary>
    /// Binds a profile to this window and records it as the last-active profile (§16.2). Only writes when
    /// the value actually changes, to avoid a save storm.
    /// </summary>
    public void LoadProfile(ServerPreferences profile)
    {
        CurrentProfile = profile;

        var userPrefs = _userPrefs.LoadPreferences();
        if (userPrefs.LastActiveProfile != profile.ProfileName)
        {
            userPrefs.LastActiveProfile = profile.ProfileName;
            _userPrefs.SavePreferences(userPrefs);
        }
    }

    // ===== Core event handlers (marshalled + guarded) =====

    private void OnServerStatusChanged(object? sender, ServerStatus status)
        => RunOnUi(() => ServerStatus = status);

    private void OnUpdateCheckStarted(object? sender, EventArgs e)
        => RunOnUi(() =>
        {
            UpdateStatusText = "Checking for updates…";
            UpdateIsLink = false;
            _updateLinkTarget = null;
        });

    private void OnUpdateCheckFinished(object? sender, SoftwareUpdateEventArgs e)
        => RunOnUi(() =>
        {
            if (!e.IsSuccessful)
            {
                UpdateStatusText = "Update check failed";
                _updateLinkTarget = AppConstants.UrlReleases;
                UpdateIsLink = true;
                return;
            }

            // Positive comparison → the fetched version is newer than the running app.
            if (AssemblyHelper.CompareVersion(e.LatestVersion!) > 0)
            {
                UpdateStatusText = $"Update available: {e.LatestVersion}";
                _updateLinkTarget = AppConstants.UrlReleases;
                UpdateIsLink = true;
            }
            else
            {
                UpdateStatusText = "Up to date";
                _updateLinkTarget = null;
                UpdateIsLink = false;
            }
        });

    private void OnServerPreferencesSaved(object? sender, System.Collections.Generic.List<ServerPreferences> profiles)
        => RunOnUi(RefreshProfiles);

    private void RefreshProfiles()
    {
        var names = _serverPrefs.LoadPreferences()
            .Select(p => p.ProfileName)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Profiles.Clear();
        foreach (var name in names) Profiles.Add(name);

        OnPropertyChanged(nameof(HasProfiles));
        LoadProfileCommand.NotifyCanExecuteChanged();
    }

    protected override void DisposeCore()
    {
        _server.StatusChanged -= OnServerStatusChanged;
        _updateProvider.UpdateCheckStarted -= OnUpdateCheckStarted;
        _updateProvider.UpdateCheckFinished -= OnUpdateCheckFinished;
        _serverPrefs.PreferencesSaved -= OnServerPreferencesSaved;
        _server.Dispose();
    }
}
