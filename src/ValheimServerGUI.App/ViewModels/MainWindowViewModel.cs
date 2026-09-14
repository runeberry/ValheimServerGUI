using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ValheimServerGUI.App.Views.Dialogs;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Logging;

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
    WorldPreferences,
}

/// <summary>The user's answer to the "host a cloud world?" prompt (§ Steam Cloud import).</summary>
public enum CloudImportChoice
{
    Move,
    Copy,
    Cancel,
}

/// <summary>
/// The update-check outcome shown in the status bar. The single source of truth the status text and the
/// status-bar icon both derive from (mirrors the WinForms status icon set).
/// </summary>
public enum UpdateCheckStatus
{
    None,
    Checking,
    UpToDate,
    Available,
    PreRelease,
    Error,
}

/// <summary>
/// One server window's view-model (§10.5). Owns a transient <see cref="ValheimServer"/> over the shared
/// singleton providers. Exposes the chrome surface (status + the single Can*/AllowServerChanges gate,
/// update status, profile list, menu/button commands) and the editable form (<see cref="Form"/>) plus the
/// StartServer flow (cloud import → validate → port check → new-world checks → start → save-on-start →
/// reselection). Window/dialog interactions are surfaced as events/delegates the code-behind wires.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IServerManager _serverManager;
    private ValheimServer? _currentServer;
    private readonly IUserPreferencesProvider _userPrefs;
    private readonly IServerPreferencesProvider _serverPrefs;
    private readonly IWorldPreferencesProvider _worldPrefs;
    private readonly ISteamCloudWorldProvider _cloudProvider;
    private readonly IIpAddressProvider _ipProvider;
    private readonly ISoftwareUpdateProvider _updateProvider;
    private readonly IShellLauncher _shell;
    private readonly IValheimPathResolver _pathResolver;

    private string? _updateLinkTarget;
    private string? _startedNewWorld;

    public MainWindowViewModel(
        IServerManager serverManager,
        IUserPreferencesProvider userPrefs,
        IServerPreferencesProvider serverPrefs,
        IWorldPreferencesProvider worldPrefs,
        ISteamCloudWorldProvider cloudProvider,
        IIpAddressProvider ipProvider,
        IPlayerDataRepository playerRepo,
        IApplicationLogger appLogger,
        ISoftwareUpdateProvider updateProvider,
        IShellLauncher shell,
        IValheimPathResolver pathResolver)
    {
        _serverManager = serverManager;
        _userPrefs = userPrefs;
        _serverPrefs = serverPrefs;
        _worldPrefs = worldPrefs;
        _cloudProvider = cloudProvider;
        _ipProvider = ipProvider;
        _updateProvider = updateProvider;
        _shell = shell;
        _pathResolver = pathResolver;

        // No server is bound until the first LoadProfile → RetargetTo (the window is always loaded with a
        // profile immediately after construction). ServerStatus defaults to Stopped, which the gates expect.
        StartAction = options => _currentServer!.Start(options);

        Details = new ServerDetailsViewModel(ipProvider, () => Form.Port);
        Players = new PlayersViewModel(playerRepo);
        Logs = new LogsViewModel(appLogger, shell, pathResolver);

        _updateProvider.UpdateCheckStarted += OnUpdateCheckStarted;
        _updateProvider.UpdateCheckFinished += OnUpdateCheckFinished;
        _serverPrefs.PreferencesSaved += OnServerPreferencesSaved;

        // The "choose an existing world" gate depends on the world list being non-empty.
        Form.Worlds.CollectionChanged += (_, _) => OnPropertyChanged(nameof(CanSelectExistingWorld));

        // The startup update check runs before this window exists, so its live events are missed. Seed the
        // readout from the provider's last result; a still-running or future check updates it via the events.
        if (_updateProvider.LastResult is { } lastResult)
            ApplyUpdateResult(lastResult);

        RefreshProfiles();
    }

    // --- events / delegates the code-behind routes ---
    public event Action? NewWindowRequested;
    public event Action? CloseRequested;
    public event Action<MenuAction>? MenuActionRequested;   // dialog opens — Wave 6
    public event Action<string>? RemoveProfileRequested;    // profile name — Wave 6
    public event Action? StopTimedOutWarning;               // §16.2 save-loss warning

    /// <summary>Shows the Move/Copy/Cancel cloud-import prompt. Wired by the window; returns Cancel if unset.</summary>
    public Func<string, Task<CloudImportChoice>>? CloudImportPrompt { get; set; }

    /// <summary>Surfaces a start-server error (manual start only). Wired by the window.</summary>
    public Action<string>? ErrorReported { get; set; }

    /// <summary>After a MANUAL update check, asks "…go to the download page?" (returns true for yes). Wired by the window.</summary>
    public Func<string, Task<bool>>? UpdateResultPrompt { get; set; }

    /// <summary>Shows the Save / Don't Save / Cancel unsaved-changes prompt (§13.3). Wired by the window.</summary>
    public Func<Task<UnsavedChangesChoice>>? UnsavedChangesPrompt { get; set; }

    /// <summary>The actual "start the server" step; overridable in tests so the full flow runs without launching.</summary>
    internal Action<IValheimServerOptions> StartAction { get; set; }

    /// <summary>The server currently targeted by this window (the selected profile's server).</summary>
    public ValheimServer? Server => _currentServer;

    public bool AutoStartOnLoad { get; set; }

    /// <summary>The editable Server Controls + Advanced Controls form.</summary>
    public ServerFormViewModel Form { get; } = new();

    /// <summary>Server Details tab.</summary>
    public ServerDetailsViewModel Details { get; }

    /// <summary>Players tab.</summary>
    public PlayersViewModel Players { get; }

    /// <summary>Logs tab.</summary>
    public LogsViewModel Logs { get; }

    // --- profile identity (single CurrentProfile source) ---
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    private ServerPreferences? _currentProfile;

    public string Title => CurrentProfile is null
        ? AppConstants.ProductName
        : $"{AppConstants.ProductName} — {CurrentProfile.ProfileName}";

    public ObservableCollection<string> Profiles { get; } = new();

    public bool HasProfiles => Profiles.Count > 0;

    // --- server status + the single gate ---
    // Profile-management commands (New Profile / Load Profile) are intentionally NOT gated by server state —
    // switching profiles is allowed while a server runs. Only Start/Stop/Restart re-raise on status change.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanStart), nameof(CanStop), nameof(CanRestart), nameof(AllowServerChanges), nameof(CanSelectExistingWorld), nameof(StatusText))]
    [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(StopCommand), nameof(RestartCommand))]
    private ServerStatus _serverStatus;

    public bool CanStart => ServerStatus == ServerStatus.Stopped;
    public bool CanStop => ServerStatus is ServerStatus.Starting or ServerStatus.Running;
    public bool CanRestart => ServerStatus == ServerStatus.Running;

    /// <summary>The single "server changes allowed" gate: fields are editable only while Stopped (§10.2).</summary>
    public bool AllowServerChanges => ServerStatus == ServerStatus.Stopped;

    /// <summary>The existing-world dropdown is usable only when changes are allowed and worlds exist;
    /// with no worlds it shows a disabled "-- No worlds --" empty state.</summary>
    public bool CanSelectExistingWorld => AllowServerChanges && Form.Worlds.Count > 0;

    public string StatusText => ServerStatus.ToString();

    // --- update-check status ---
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UpdateLinkCommand))]
    private string _updateStatusText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UpdateLinkCommand))]
    private bool _updateIsLink;

    /// <summary>The update-check outcome the status-bar icon derives from (see the status text below).</summary>
    [ObservableProperty]
    private UpdateCheckStatus _updateStatus;

    // ===== commands =====

    [RelayCommand(CanExecute = nameof(CanStart))]
    private Task Start() => StartServerAsync(isManual: true);

    [RelayCommand(CanExecute = nameof(CanStop))]
    private void Stop() => _currentServer?.Stop();

    [RelayCommand(CanExecute = nameof(CanRestart))]
    private void Restart() => _currentServer?.Restart();

    [RelayCommand]
    private void NewWindow() => NewWindowRequested?.Invoke();

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();

    [RelayCommand]
    private void NewProfile() => MenuActionRequested?.Invoke(MenuAction.NewProfile);

    [RelayCommand]
    private void SaveProfile() => MenuActionRequested?.Invoke(MenuAction.SaveProfile);

    [RelayCommand]
    private void SaveProfileAs() => MenuActionRequested?.Invoke(MenuAction.SaveProfileAs);

    // Ungated: switching profiles is allowed even while a server is running (the switch re-targets the
    // window; the previous profile's server keeps running in the background). Routed through the same
    // unsaved-changes guard as the dropdown so File > Load and the dropdown share one switch plumbing.
    //
    // Parameter is object? (not string): the dynamic Load/Remove Profile submenus bind CommandParameter via
    // {Binding}, which transiently resolves to the inherited (VM) DataContext while a generated item's
    // container is being set up — before its string DataContext lands. RelayCommand<string> throws on that
    // wrong-typed argument in CanExecute and crashes the menu, so the commands accept any parameter and
    // no-op on a non-string.
    [RelayCommand]
    private Task LoadProfile(object? profileName)
        => profileName is string name ? RequestSwitchProfileAsync(name) : Task.CompletedTask;

    [RelayCommand]
    private void RemoveProfile(object? profileName)
    {
        if (profileName is string name) RemoveProfileRequested?.Invoke(name);
    }

    [RelayCommand]
    private void Preferences() => MenuActionRequested?.Invoke(MenuAction.Preferences);

    [RelayCommand]
    private void SetDirectories() => MenuActionRequested?.Invoke(MenuAction.SetDirectories);

    [RelayCommand]
    private void WorldPreferences() => MenuActionRequested?.Invoke(MenuAction.WorldPreferences);

    [RelayCommand]
    private void RefreshWorlds() => RefreshWorldList();

    [RelayCommand]
    private void OpenSaveFolder()
    {
        try
        {
            _shell.OpenDirectory(BuildOptions().GetValidatedSaveDataFolder().FullName);
        }
        catch (Exception ex)
        {
            ErrorReported?.Invoke($"Unable to open the save folder: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenServerExeFolder()
    {
        try
        {
            var exe = BuildOptions().GetValidatedServerExe().FullName;
            _shell.OpenDirectory(exe);
        }
        catch (Exception ex)
        {
            ErrorReported?.Invoke($"Unable to open the server folder: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenSettingsDirectory()
    {
        var dir = Path.GetDirectoryName(_pathResolver.UserPrefsFilePath);
        if (!string.IsNullOrEmpty(dir)) _shell.OpenDirectory(dir);
    }

    [RelayCommand]
    private void OnlineManual() => _shell.OpenWebAddress(AppConstants.UrlHelp);

    [RelayCommand]
    private void CharacterNamesHelp() => _shell.OpenWebAddress(AppConstants.UrlHelpCharacterNames);

    [RelayCommand]
    private void PortForwarding() => _shell.OpenWebAddress(AppConstants.UrlPortForwarding);

    [RelayCommand]
    private void BugReport() => MenuActionRequested?.Invoke(MenuAction.BugReport);

    [RelayCommand]
    private async Task CheckForUpdates() => await _updateProvider.CheckForUpdatesAsync(isManualCheck: true);

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

    // ===== profile / form loading =====

    /// <summary>
    /// Switches this window to <paramref name="name"/>, honouring the unsaved-changes guard. The single switch
    /// plumbing both File &gt; Load and the menu-bar dropdown call. Returns false only when the user cancels
    /// (so the dropdown can revert); a no-op switch to the current profile returns true.
    /// </summary>
    public async Task<bool> RequestSwitchProfileAsync(string name)
    {
        if (name == CurrentProfile?.ProfileName) return true;
        if (!await ResolveUnsavedChangesAsync()) return false;

        var prefs = _serverPrefs.LoadPreferences(name);
        if (prefs is not null) LoadProfile(prefs);
        return true;
    }

    /// <summary>The unsaved-changes guard for New Profile / Save As (code-behind). False = the user cancelled.</summary>
    public Task<bool> ConfirmDiscardCurrentAsync() => ResolveUnsavedChangesAsync();

    // Shared guard: when the form is dirty, prompt Save / Discard / Cancel. Save persists the current
    // profile then proceeds; Discard proceeds; Cancel aborts (returns false). No prompt when clean.
    private async Task<bool> ResolveUnsavedChangesAsync()
    {
        if (!Form.IsDirty || UnsavedChangesPrompt is null) return true;

        switch (await UnsavedChangesPrompt())
        {
            case UnsavedChangesChoice.Save:
                _serverPrefs.SavePreferences(BuildPreferences());
                return true;
            case UnsavedChangesChoice.Discard:
                return true;
            default: // Cancel
                return false;
        }
    }

    /// <summary>
    /// Binds a profile to this window: records it as last-active (§16.2), loads the form fields, and lists
    /// the worlds for its save folder.
    /// </summary>
    public void LoadProfile(ServerPreferences profile)
    {
        CurrentProfile = profile;

        // Re-target the window's server/status/tabs onto this profile's server before loading form fields.
        RetargetTo(profile.ProfileName);

        var userPrefs = _userPrefs.LoadPreferences();
        if (userPrefs.LastActiveProfile != profile.ProfileName)
        {
            userPrefs.LastActiveProfile = profile.ProfileName;
            _userPrefs.SavePreferences(userPrefs);
        }

        Form.LoadFieldsFrom(profile);
        RefreshWorldList();
        SelectWorld(profile.WorldName);
    }

    /// <summary>
    /// Points this window at the selected profile's server (created on first ask, shared app-wide). Swaps the
    /// event subscriptions, re-seeds every status-derived gate from the new server's status, and re-targets
    /// the Details/Logs tabs. Runs on the UI thread (always called from a UI action via LoadProfile). Players
    /// is deliberately not re-targeted — it is a global, merged player list (documented limitation).
    /// </summary>
    private void RetargetTo(string profileName)
    {
        var next = _serverManager.GetOrCreate(profileName);
        if (ReferenceEquals(next, _currentServer)) return;

        if (_currentServer is not null)
        {
            _currentServer.StatusChanged -= HandleServerStatusChanged;
            _currentServer.StopTimedOut -= OnServerStopTimedOut;
        }

        _currentServer = next;

        // Cleared before re-seeding the status: it belonged to the previous server's start flow, and the
        // OnServerStatusChanged hook (which fires synchronously below if the new server is already Running)
        // must not act on the stale flag.
        _startedNewWorld = null;

        _currentServer.StatusChanged += HandleServerStatusChanged;
        _currentServer.StopTimedOut += OnServerStopTimedOut;

        // Mandatory: re-seed the single status gate from the newly selected server so Can*/AllowServerChanges
        // and the Start/Stop/Restart command CanExecute reflect that server immediately, with no event.
        ServerStatus = _currentServer.Status;

        Details.SetServer(_currentServer);
        Logs.SetServerLog(_serverManager.GetServerLog(profileName));
    }

    // ===== StartServer flow (§10.3 / GetServerOptionsFromFormState + validation) =====

    public async Task StartServerAsync(bool isManual)
    {
        void Error(string message)
        {
            if (isManual) ErrorReported?.Invoke(message);
        }

        // A selected cloud world must be imported into the local save folder before it can be hosted; the
        // suffix never reaches options/prefs/validation.
        if (Form.IsSelectedWorldCloud)
        {
            var cloudName = ServerFormViewModel.StripCloudSuffix(Form.ExistingWorld)!;
            var choice = CloudImportPrompt is not null ? await CloudImportPrompt(cloudName) : CloudImportChoice.Cancel;
            if (choice == CloudImportChoice.Cancel) return;

            try
            {
                var cloudSaveFolder = BuildOptions().GetValidatedSaveDataFolder();
                _cloudProvider.ImportCloudWorld(cloudName, cloudSaveFolder, move: choice == CloudImportChoice.Move);
            }
            catch (Exception ex)
            {
                Error($"Failed to import cloud world '{cloudName}': {ex.Message}");
                return;
            }

            RefreshWorldList();
            Form.UseNewWorld = false;
            Form.ExistingWorld = cloudName;
        }

        var options = BuildOptions();

        try
        {
            options.Validate();
        }
        catch (Exception ex)
        {
            Error(ex.Message);
            return;
        }

        var port = options.Port;
        if (!_ipProvider.IsLocalUdpPortAvailable(port, port + 1))
        {
            Error($"Port {port} or {port + 1} is already in use.\n" +
                  "Valheim requires two adjacent ports to run a dedicated server.\n" +
                  "Please shut down any UDP applications using these ports, or choose a different port for your server.");
            return;
        }

        var worldName = options.WorldName ?? string.Empty;
        var saveFolder = options.GetValidatedSaveDataFolder();
        var newWorld = Form.UseNewWorld;

        if (newWorld)
        {
            if (string.IsNullOrWhiteSpace(worldName))
            {
                Error("You must enter a world name, or choose an existing world.");
                return;
            }
            if (worldName.Length < 5 || worldName.Length > 20)
            {
                Error("World name must be 5-20 characters long.");
                return;
            }
            if (!saveFolder.IsWorldNameAvailable(worldName))
            {
                Error($"A world named '{worldName}' already exists.");
                Form.UseNewWorld = false;
                Form.ExistingWorld = worldName;
                return;
            }
        }
        else if (saveFolder.IsWorldNameAvailable(worldName))
        {
            Error($"No world exists with name '{worldName}'.");
            return;
        }

        try
        {
            StartAction(options);
        }
        catch (Exception ex)
        {
            Error(ex.Message);
            return;
        }

        // Remember a just-started new world so we can reselect it as "existing" once it comes up (§10.2).
        _startedNewWorld = newWorld ? worldName : null;

        if (_userPrefs.LoadPreferences().SaveProfileOnStart)
            _serverPrefs.SavePreferences(BuildPreferences());
    }

    /// <summary>GetServerOptionsFromFormState.</summary>
    public ValheimServerOptions BuildOptions()
    {
        var userPrefs = _userPrefs.LoadPreferences();
        var serverPrefs = BuildPreferences();

        var options = new ValheimServerOptions
        {
            Name = serverPrefs.Name,
            Password = serverPrefs.Password,
            PasswordValidation = userPrefs.EnablePasswordValidation,
            WorldName = serverPrefs.WorldName,
            Public = serverPrefs.Public,
            Port = serverPrefs.Port,
            Crossplay = serverPrefs.Crossplay,
            SaveInterval = serverPrefs.SaveInterval,
            Backups = serverPrefs.BackupCount,
            BackupShort = serverPrefs.BackupIntervalShort,
            BackupLong = serverPrefs.BackupIntervalLong,
            AdditionalArgs = serverPrefs.AdditionalArgs,
            ServerExePath = !string.IsNullOrWhiteSpace(serverPrefs.ServerExePath)
                ? serverPrefs.ServerExePath
                : userPrefs.ServerExePath,
            SaveDataFolderPath = !string.IsNullOrWhiteSpace(serverPrefs.SaveDataFolderPath)
                ? serverPrefs.SaveDataFolderPath
                : userPrefs.SaveDataFolderPath,
            LogToFile = serverPrefs.WriteServerLogsToFile,
            // Lines land in the profile's server-owned buffer regardless of which window started the server.
            LogMessageHandler = _serverManager.GetLogAppender(
                CurrentProfile?.ProfileName ?? CoreConstants.DefaultServerProfileName),
        };

        var worldName = serverPrefs.WorldName;
        if (!string.IsNullOrWhiteSpace(worldName))
        {
            var worldPrefs = _worldPrefs.LoadPreferences(worldName);
            if (worldPrefs is not null)
            {
                if (!string.IsNullOrEmpty(worldPrefs.Preset))
                    options.WorldPreset = worldPrefs.Preset;
                else
                    options.WorldModifiers = worldPrefs.Modifiers;

                options.WorldKeys = worldPrefs.Keys;
            }
        }

        return options;
    }

    /// <summary>GetPrefsFromFormState — merged onto the existing/new profile prefs.</summary>
    public ServerPreferences BuildPreferences()
    {
        var profileName = CurrentProfile?.ProfileName ?? CoreConstants.DefaultServerProfileName;
        var prefs = _serverPrefs.LoadPreferences(profileName) ?? new ServerPreferences { ProfileName = profileName };
        return Form.ToPreferences(prefs);
    }

    private void RefreshWorldList()
    {
        // Preserve the current selection across the rebuild (a manual Refresh must not blank the picker).
        var previous = Form.ExistingWorld;

        List<string> local;
        try
        {
            local = BuildOptions().GetValidatedSaveDataFolder().GetWorldNames();
        }
        catch
        {
            // Save folder not configured/available yet — nothing to list. App-driven, so not a user edit.
            Form.RunClean(() =>
            {
                Form.Worlds.Clear();
                Form.ExistingWorld = null;
            });
            return;
        }

        // Also surface Steam Cloud worlds so they can be imported and hosted; local wins on a name clash.
        var cloud = _cloudProvider.GetCloudWorldNames()
            .Where(n => !local.Contains(n, StringComparer.OrdinalIgnoreCase))
            .Select(n => n + AppConstants.CloudWorldSuffix);

        // App-driven list maintenance must not trip the form's dirty flag — only direct user edits do.
        Form.RunClean(() =>
        {
            Form.Worlds.Clear();
            foreach (var world in local.Concat(cloud))
                Form.Worlds.Add(world);

            // Re-select the prior world if it survived the refresh; otherwise fall back to the first one so
            // the dropdown never lands on an empty selection while worlds exist.
            Form.ExistingWorld = previous is not null && Form.Worlds.Contains(previous)
                ? previous
                : Form.Worlds.FirstOrDefault();
        });
    }

    private void SelectWorld(string? worldName) => Form.RunClean(() =>
    {
        if (string.IsNullOrWhiteSpace(worldName))
        {
            Form.UseNewWorld = false;
            Form.ExistingWorld = Form.Worlds.FirstOrDefault();
            return;
        }

        var match = Form.Worlds.FirstOrDefault(w =>
            string.Equals(ServerFormViewModel.StripCloudSuffix(w), worldName, StringComparison.OrdinalIgnoreCase));

        if (match is not null)
        {
            Form.UseNewWorld = false;
            Form.ExistingWorld = match;
        }
        else
        {
            Form.UseNewWorld = true;
            Form.NewWorldName = worldName;
        }
    });

    // ===== Core event handlers (marshalled + guarded) =====

    private void HandleServerStatusChanged(object? sender, ServerStatus status)
        => RunOnUi(() => ServerStatus = status);

    // A just-started new world now exists; switch the picker back to Existing and select it (§10.2).
    partial void OnServerStatusChanged(ServerStatus value)
    {
        if (value == ServerStatus.Running && _startedNewWorld is not null)
        {
            RefreshWorldList();
            SelectWorld(_startedNewWorld);
            _startedNewWorld = null;
        }
    }

    private void OnServerStopTimedOut(object? sender, EventArgs e)
        => RunOnUi(() => StopTimedOutWarning?.Invoke());

    private void OnUpdateCheckStarted(object? sender, EventArgs e)
        => RunOnUi(() =>
        {
            UpdateStatusText = "Checking for updates…";
            UpdateStatus = UpdateCheckStatus.Checking;
            UpdateIsLink = false;
            _updateLinkTarget = null;
        });

    private void OnUpdateCheckFinished(object? sender, SoftwareUpdateEventArgs e)
        => RunOnUi(() =>
        {
            ApplyUpdateResult(e);
            if (e.IsManualCheck) _ = PromptManualUpdateResultAsync(e);
        });

    // A manual "Check for Updates" reports its result in a dialog and offers to open the download page (parity).
    private async Task PromptManualUpdateResultAsync(SoftwareUpdateEventArgs e)
    {
        if (UpdateResultPrompt is null) return;

        var body = $"{BuildManualUpdateMessage(e)}\nWould you like to go to the download page?";
        if (await UpdateResultPrompt(body))
            _shell.OpenWebAddress(AppConstants.UrlReleases);
    }

    private static string BuildManualUpdateMessage(SoftwareUpdateEventArgs e)
    {
        if (!e.IsSuccessful)
            return $"Update check failed: {e.Exception?.GetPrimaryException().Message}.";

        return AssemblyHelper.CompareVersion(e.LatestVersion!) switch
        {
            > 0 => "A newer version of ValheimServerGUI is available.",
            0 => "You are running the latest version of ValheimServerGUI.",
            -1 => "You are currently running a pre-release version of ValheimServerGUI. " +
                  $"The latest stable version is ({e.LatestVersion}).",
            _ => $"Update check failed: Unable to parse version ({e.LatestVersion}).",
        };
    }

    /// <summary>Startup check: the validation error if the configured server exe is missing, else null.</summary>
    public string? GetMissingServerExeError()
    {
        try
        {
            BuildOptions().GetValidatedServerExe();
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    // Mirror the WinForms status-bar logic: show the compared version in the text, and distinguish
    // up-to-date / update-available / pre-release / parse-failure (CompareVersion returns 1 / 0 / -1 / -2).
    private void ApplyUpdateResult(SoftwareUpdateEventArgs e)
    {
        if (!e.IsSuccessful)
        {
            UpdateStatusText = "Update check failed";
            UpdateStatus = UpdateCheckStatus.Error;
            _updateLinkTarget = AppConstants.UrlReleases;
            UpdateIsLink = true;
            return;
        }

        switch (AssemblyHelper.CompareVersion(e.LatestVersion!))
        {
            case > 0:
                UpdateStatusText = $"Update available ({e.LatestVersion})";
                UpdateStatus = UpdateCheckStatus.Available;
                _updateLinkTarget = AppConstants.UrlReleases;
                UpdateIsLink = true;
                break;
            case 0:
                UpdateStatusText = $"Up to date ({e.LatestVersion})";
                UpdateStatus = UpdateCheckStatus.UpToDate;
                _updateLinkTarget = null;
                UpdateIsLink = false;
                break;
            case -1:
                UpdateStatusText = $"Pre-release build ({AssemblyHelper.GetApplicationVersion()})";
                UpdateStatus = UpdateCheckStatus.PreRelease;
                _updateLinkTarget = null;
                UpdateIsLink = false;
                break;
            default:
                UpdateStatusText = $"Unable to parse version ({e.LatestVersion})";
                UpdateStatus = UpdateCheckStatus.Error;
                _updateLinkTarget = AppConstants.UrlReleases;
                UpdateIsLink = true;
                break;
        }
    }

    private void OnServerPreferencesSaved(object? sender, List<ServerPreferences> profiles)
        => RunOnUi(RefreshProfiles);

    private void RefreshProfiles()
    {
        var names = _serverPrefs.LoadPreferences()
            .Select(p => p.ProfileName)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Reconcile in place rather than Clear()+Add(). A profile switch saves LastActiveProfile, which
        // chains UserPrefs-saved → ServerPrefs.PreferencesSaved → here; a Clear() would momentarily drop the
        // menu-bar dropdown's selected item out of the collection, blanking the ComboBox on every switch even
        // though the name set is unchanged. So only remove names that are gone and insert genuinely new ones.
        for (var i = Profiles.Count - 1; i >= 0; i--)
            if (!names.Contains(Profiles[i], StringComparer.OrdinalIgnoreCase))
                Profiles.RemoveAt(i);

        foreach (var name in names)
        {
            if (Profiles.Contains(name, StringComparer.OrdinalIgnoreCase)) continue;
            var index = 0;
            while (index < Profiles.Count && StringComparer.OrdinalIgnoreCase.Compare(Profiles[index], name) < 0)
                index++;
            Profiles.Insert(index, name);
        }

        OnPropertyChanged(nameof(HasProfiles));
    }

    protected override void DisposeCore()
    {
        // Unsubscribe from the currently-targeted server, but do NOT dispose it — servers are owned by the
        // IServerManager and outlive this window (a missed -= would leak the whole view-model).
        if (_currentServer is not null)
        {
            _currentServer.StatusChanged -= HandleServerStatusChanged;
            _currentServer.StopTimedOut -= OnServerStopTimedOut;
        }
        _updateProvider.UpdateCheckStarted -= OnUpdateCheckStarted;
        _updateProvider.UpdateCheckFinished -= OnUpdateCheckFinished;
        _serverPrefs.PreferencesSaved -= OnServerPreferencesSaved;
        Details.Dispose();
        Players.Dispose();
        Logs.Dispose();
    }
}
