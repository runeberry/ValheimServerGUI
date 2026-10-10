using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ValheimServerGUI.App.Converters;
using ValheimServerGUI.App.Views.Dialogs;
using ValheimServerGUI.Game;
using ValheimServerGUI.Localization;
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
    private readonly IApplicationLogger _logger;
    private readonly IPlayerListImportService _import;

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
        IValheimPathResolver pathResolver,
        IPlayerListImportService import)
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
        _logger = appLogger;
        _import = import;

        // No server is bound until the first LoadProfile → RetargetTo (the window is always loaded with a
        // profile immediately after construction). ServerStatus defaults to Stopped, which the gates expect.
        StartAction = options => _currentServer!.Start(options);

        Details = new ServerDetailsViewModel(ipProvider, () => Form.Port);
        // The Players tab edits the active profile's role overrides/mode, which live on the form, and reads the
        // app-global player defaults from user preferences; the three list files are generated from the resolved
        // roles at server start (in Core), so the tab needs no access-list service here.
        Players = new PlayersViewModel(playerRepo, Form, userPrefs);
        Logs = new LogsViewModel(appLogger, shell, pathResolver);

        _updateProvider.UpdateCheckStarted += OnUpdateCheckStarted;
        _updateProvider.UpdateCheckFinished += OnUpdateCheckFinished;
        _serverPrefs.PreferencesSaved += OnServerPreferencesSaved;

        // Saving the Manage Players defaults re-resolves every running server's roles (live-apply).
        _userPrefs.PreferencesSaved += OnUserPreferencesSaved;

        // A running server adopted list-file changes made during play (in-game bans, hand edits): persist them.
        _serverManager.PlayerRolesAdopted += OnPlayerRolesAdopted;

        // The "choose an existing world" gate depends on the world list being non-empty.
        Form.Worlds.CollectionChanged += (_, _) => OnPropertyChanged(nameof(CanSelectExistingWorld));

        // Player roles are the one carve-out from "changes only while stopped": apply them live to a running
        // server. Only genuine user edits raise this (loads run under RunClean).
        Form.PlayerRolesEdited += OnPlayerRolesEdited;

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

    /// <summary>Shows the Save Changes / Discard Changes / Cancel unsaved-changes prompt (§13.3). Wired by the window.</summary>
    public Func<Task<UnsavedChangesChoice>>? UnsavedChangesPrompt { get; set; }

    /// <summary>Shows a single-OK informational modal (title, body). Wired by the window; no-op if unset.</summary>
    public Func<string, string, Task>? MessagePrompt { get; set; }

    /// <summary>Shows the Continue/Cancel import confirmation (returns true for Continue). Wired by the window.</summary>
    public Func<string, Task<bool>>? ImportConfirmPrompt { get; set; }

    /// <summary>Shows the 3-way role-conflict prompt. Wired by the window; treated as UseServerProfile if unset.</summary>
    public Func<string, Task<RoleConflictChoice>>? ConflictPrompt { get; set; }

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

    public string StatusText => EnumDisplayConverter.ToText(ServerStatus);

    // --- update-check status ---
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UpdateLinkCommand))]
    private string _updateStatusText = string.Empty;

    /// <summary>The release page the update readout links to; null when it is plain text.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UpdateIsLink))]
    [NotifyCanExecuteChangedFor(nameof(UpdateLinkCommand))]
    private string? _updateLinkTarget;

    public bool UpdateIsLink => UpdateLinkTarget is not null;

    /// <summary>The update-check outcome the status-bar icon derives from (see the status text below).</summary>
    [ObservableProperty]
    private UpdateCheckStatus _updateStatus;

    // ===== commands =====

    [RelayCommand(CanExecute = nameof(CanStart))]
    private Task Start() => StartServerAsync(isManual: true);

    [RelayCommand(CanExecute = nameof(CanStop))]
    private void Stop()
    {
        _logger.Information("Stopping server for profile '{profile}'", CurrentProfile?.ProfileName);
        _currentServer?.Stop();
    }

    [RelayCommand(CanExecute = nameof(CanRestart))]
    private void Restart()
    {
        _logger.Information("Restarting server for profile '{profile}'", CurrentProfile?.ProfileName);
        _currentServer?.Restart();
    }

    [RelayCommand]
    private void NewWindow()
    {
        _logger.Information("Opening a new window");
        NewWindowRequested?.Invoke();
    }

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
            ErrorReported?.Invoke(string.Format(Strings.Validation_OpenSaveFolderFailed, ex.Message));
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
            ErrorReported?.Invoke(string.Format(Strings.Validation_OpenServerFolderFailed, ex.Message));
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

    // Always enabled (imports touch only roles, which are editable regardless of server state).
    [RelayCommand]
    private Task ImportPlayerLists() => RunImportAsync(interactive: true);

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
        if (UpdateLinkTarget is { } url)
            _shell.OpenWebAddress(url);
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
        _logger.Information("Loading server profile '{profile}'", profile.ProfileName);
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

        // First-launch adoption: when no roles are configured at all (no overrides on this profile and no global
        // defaults) silently import any existing list files (no modals, assume Continue, errors only logged) so
        // upgrading users keep their setups. Once defaults exist this must not run: files generated before a
        // default was added would pin that player to None. The silent path has no awaited steps, so this
        // completes synchronously before the world refresh below.
        if (profile.PlayerRoles.Count == 0 && LoadPlayerDefaults().Count == 0)
        {
            _logger.Information("Profile '{profile}' has no player roles; attempting first-launch list import.", profile.ProfileName);
            _ = RunImportAsync(interactive: false);
        }

        RefreshWorldList();
        SelectWorld(profile.WorldName);
    }

    /// <summary>
    /// Points this window at the selected profile's server (created on first ask, shared app-wide). Swaps the
    /// event subscriptions, re-seeds every status-derived gate from the new server's status, and re-targets
    /// the Details/Logs tabs. Runs on the UI thread (always called from a UI action via LoadProfile). The
    /// Players tab follows the active profile automatically: its roles/mode live on the form, which
    /// LoadProfile reloads (firing RoleStateChanged) right after this call.
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
            _logger.Warning("Cannot start server for profile '{profile}': {message}", CurrentProfile?.ProfileName, message);
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
                Error(string.Format(Strings.Validation_CloudImportFailed, cloudName, ex.Message));
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
            Error(string.Format(Strings.Validation_PortInUse, port, port + 1));
            return;
        }

        var worldName = options.WorldName ?? string.Empty;
        var saveFolder = options.GetValidatedSaveDataFolder();
        var newWorld = Form.UseNewWorld;

        if (newWorld)
        {
            if (string.IsNullOrWhiteSpace(worldName))
            {
                Error(Strings.Validation_WorldNameRequired);
                return;
            }
            if (worldName.Length < 5 || worldName.Length > 20)
            {
                Error(Strings.Validation_WorldNameLength);
                return;
            }
            if (!saveFolder.IsWorldNameAvailable(worldName))
            {
                Error(string.Format(Strings.Validation_WorldNameTaken, worldName));
                Form.UseNewWorld = false;
                Form.ExistingWorld = worldName;
                return;
            }
        }
        else if (saveFolder.IsWorldNameAvailable(worldName))
        {
            Error(string.Format(Strings.Validation_WorldNotFound, worldName));
            return;
        }

        // List-file safety runs BEFORE Core writes the files: adopt changes made to them since VSG's last write,
        // resolve conflicts, and guard a stray permitted-list file. It may change the form's roles, so rebuild the
        // options afterwards so both the launch and save-on-start reflect the merged config.
        if (!await ResolveListSafetyBeforeStartAsync(options, isManual)) return;
        options = BuildOptions();

        _logger.Information("Starting {mode} server for profile '{profile}' on port {port} (world: {world})",
            isManual ? "manual" : "auto-start", CurrentProfile?.ProfileName, port, worldName);

        try
        {
            StartAction(options);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to start server for profile '{profile}': {message}", CurrentProfile?.ProfileName, ex.Message);
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
            // Access-list inputs: the mode flag, the profile's role overrides, and the global defaults they layer
            // over. Core resolves and writes these to the three gating files.
            UsePermittedList = serverPrefs.UsePermittedList,
            RoleOverrides = new Dictionary<string, PlayerRoleEntry>(serverPrefs.PlayerRoles),
            RoleDefaults = new Dictionary<string, PlayerDefaultEntry>(userPrefs.PlayerDefaults),
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

    private IReadOnlyDictionary<string, PlayerDefaultEntry> LoadPlayerDefaults()
        => _userPrefs.LoadPreferences().PlayerDefaults;

    // ===== Player-list import / startup conflict detection =====

    // Reuses the save-folder resolution BuildOptions performs (profile SaveDataFolderPath ?? user default).
    // Returns null when no folder is configured/available yet.
    private string? ResolveSaveDataFolder()
    {
        try
        {
            return BuildOptions().GetValidatedSaveDataFolder().FullName;
        }
        catch
        {
            return null;
        }
    }

    private async Task ShowMessageAsync(string title, string body)
    {
        if (MessagePrompt is not null) await MessagePrompt(title, body);
    }

    /// <summary>
    /// Imports the on-disk list files into the profile's roles wholesale (ad-hoc button, first-launch, and the
    /// start-time "no roles yet" adoption). <paramref name="interactive"/> drives the modals: an interactive run
    /// confirms and reports via dialogs; a silent run assumes Continue and only logs (no modals, no error box).
    /// </summary>
    public async Task RunImportAsync(bool interactive = true)
    {
        var savedir = ResolveSaveDataFolder();
        if (savedir is null)
        {
            _logger.Warning("Player-list import skipped for profile '{profile}': no save folder configured.", CurrentProfile?.ProfileName);
            if (interactive) await ShowMessageAsync(Strings.Import_Title, Strings.Import_NoFiles);
            return;
        }

        // Always log which files were examined, so a "no files"/failure result is traceable.
        var dir = new DirectoryInfo(savedir);
        _logger.Information("Player-list import: checking {admin}, {banned}, {permitted}.",
            dir.GetAdminListFile().FullName, dir.GetBannedListFile().FullName, dir.GetPermittedListFile().FullName);

        ImportPlan plan;
        try
        {
            plan = _import.BuildImport(savedir, Form.PlayerRoles, Form.UsePermittedList, LoadPlayerDefaults());
        }
        catch (Exception ex)
        {
            _logger.Error("Player-list import failed: {message}", ex.Message);
            if (interactive) await ShowMessageAsync(Strings.Import_Title, Strings.Import_Failed);
            return;
        }

        foreach (var entry in plan.Unrecognized)
            _logger.Warning("Player-list import: skipped unrecognized entry {entry} (it stays in the file).", entry);

        if (!plan.AnyFilesPresent)
        {
            _logger.Information("Player-list import: no list files present in {folder}.", savedir);
            if (interactive) await ShowMessageAsync(Strings.Import_Title, Strings.Import_NoFiles);
            return;
        }

        if (plan.UpdateCount == 0)
        {
            _logger.Information("Player-list import: files already match the profile; no role changes.");
            if (interactive) await ShowMessageAsync(Strings.Import_Title, Strings.Import_NoRoles);
            return;
        }

        if (interactive)
        {
            var proceed = ImportConfirmPrompt is not null
                && await ImportConfirmPrompt(string.Format(Strings.Import_Confirm, plan.UpdateCount));
            if (!proceed)
            {
                _logger.Information("Player-list import cancelled by user.");
                return;
            }
        }

        Form.ApplyImport(plan.Roles, plan.UsePermittedList);
        _logger.Information("Player-list import applied: {count} role change(s), usePermittedList={mode}.",
            plan.UpdateCount, plan.UsePermittedList);

        if (interactive) await ShowMessageAsync(Strings.Import_Title, string.Format(Strings.Import_Updated, plan.UpdateCount));
    }

    /// <summary>
    /// Reconciles the on-disk list files with the profile before Core writes them at start. Returns false to abort
    /// the start. Three steps: (1) back up a stray non-empty permittedlist.txt in open mode (abort if it can't be
    /// moved); (2) adopt the files wholesale when no roles exist yet, else adopt what changed in them since VSG's
    /// last write; (3) on a conflict (a changed entry that disagrees with the profile), let the user keep the
    /// profile (Core backs the file up before overwriting it), keep the files' roles, or cancel. Non-interactive
    /// (auto-start or an unwired prompt) keeps the profile.
    /// </summary>
    private async Task<bool> ResolveListSafetyBeforeStartAsync(ValheimServerOptions options, bool isManual)
    {
        var dir = options.GetValidatedSaveDataFolder();
        var savedir = dir.FullName;

        // (1) Open mode, but a non-empty permitted list on disk would silently gate everyone out — move it aside.
        if (!options.UsePermittedList && _import.HasEntries(savedir, PlayerAccessList.Permitted))
        {
            var permittedFile = dir.GetPermittedListFile();
            var backup = ValheimPathExtensions.BackupListFile(permittedFile);
            if (backup is null)
            {
                _logger.Error("Cannot start: permitted list file {file} is present in open mode and could not be moved.", permittedFile.FullName);
                await ShowMessageAsync(Strings.Import_PermittedFileError_Title, string.Format(Strings.Import_PermittedFileError_Message, permittedFile.FullName));
                return false;
            }
            _logger.Warning("Open-mode start: backed up existing permitted list {src} to {dst}.", permittedFile.FullName, backup.FullName);
        }

        // (2) No roles configured at all (no overrides, no global defaults) → adopt whatever the files say (silent).
        var defaults = LoadPlayerDefaults();
        if (Form.PlayerRoles.Count == 0 && defaults.Count == 0)
        {
            await RunImportAsync(interactive: false);
            return true;
        }

        ReconcileResult report;
        try
        {
            report = _import.Reconcile(savedir, Form.PlayerRoles, defaults, externalWins: false);
        }
        catch (Exception ex)
        {
            _logger.Error("Cannot start: reading the player list files failed: {message}", ex.Message);
            return false;
        }

        foreach (var line in report.LogLines) _logger.Information("{line}", line);
        if (report.Changed) Form.ApplyImport(report.Overrides, Form.UsePermittedList);

        if (report.Conflicts.Count == 0) return true;

        var choice = (isManual && ConflictPrompt is not null)
            ? await ConflictPrompt(string.Format(Strings.Import_RoleConflict_Message, report.Conflicts.Count))
            : RoleConflictChoice.UseServerProfile; // non-interactive / unwired: the profile wins

        switch (choice)
        {
            case RoleConflictChoice.Cancel:
                _logger.Information("Server start cancelled at the role-conflict prompt.");
                return false;

            case RoleConflictChoice.UseRolesFromFile:
                var fromFiles = _import.Reconcile(savedir, Form.PlayerRoles, defaults, externalWins: true);
                foreach (var line in fromFiles.LogLines) _logger.Information("{line}", line);
                Form.ApplyImport(fromFiles.Overrides, Form.UsePermittedList);
                _logger.Information("Role conflict resolved: adopted the list files' roles into the profile.");
                return true;

            default: // UseServerProfile — Core backs up each file before dropping entries VSG didn't write.
                _logger.Information("Role conflict resolved: keeping the profile's roles (conflicting files are backed up).");
                return true;
        }
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

    // Live-apply carve-out: while the server is NOT stopped, a player-role/mode edit is committed immediately
    // (the running server re-reads the list files within seconds) instead of waiting for Save. While stopped,
    // role edits stay in the ordinary dirty/Save flow (Form.IsDirty → Save / save-on-start → generate at Start).
    private void OnPlayerRolesEdited(object? sender, EventArgs e)
    {
        if (ServerStatus == ServerStatus.Stopped) return;

        // Persist ONLY the roles + flag onto the on-disk profile, so any unsaved (stopped-only) field edits
        // stay pending rather than being silently committed by a role change.
        var profileName = CurrentProfile?.ProfileName ?? CoreConstants.DefaultServerProfileName;
        var prefs = _serverPrefs.LoadPreferences(profileName) ?? new ServerPreferences { ProfileName = profileName };
        Form.ApplyRolesTo(prefs);
        _serverPrefs.SavePreferences(prefs);

        // Regenerate the running server's list files now (reusing the start-time generation path), and keep
        // its live options in step so a later restart preserves the change.
        try
        {
            _currentServer?.ApplyPlayerRoles(
                new Dictionary<string, PlayerRoleEntry>(prefs.PlayerRoles),
                new Dictionary<string, PlayerDefaultEntry>(LoadPlayerDefaults()),
                prefs.UsePermittedList);
        }
        catch (Exception ex)
        {
            _logger.Error("Player role saved, but regenerating the running server's access lists failed: {message}", ex.Message);
        }
    }

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
            UpdateStatusText = Strings.Update_Checking;
            UpdateStatus = UpdateCheckStatus.Checking;
            UpdateLinkTarget = null;
        });

    private void OnUpdateCheckFinished(object? sender, SoftwareUpdateEventArgs e)
        => RunOnUi(() =>
        {
            ApplyUpdateResult(e);
            if (e.IsManualCheck) _ = PromptManualUpdateResultAsync(e);
        });

    // A manual "Check for Updates" reports its result in a dialog, offering to open the release page when the check
    // returned one (parity); otherwise (no release qualifies, or the check failed) it just reports.
    private async Task PromptManualUpdateResultAsync(SoftwareUpdateEventArgs e)
    {
        var message = BuildManualUpdateMessage(e);
        if (e.ReleaseUrl is not { } url)
        {
            await ShowMessageAsync(Strings.Prompt_CheckForUpdates_Title, message);
            return;
        }

        if (UpdateResultPrompt is not null && await UpdateResultPrompt(string.Format(Strings.Update_ManualPrompt, message)))
            _shell.OpenWebAddress(url);
    }

    private static string BuildManualUpdateMessage(SoftwareUpdateEventArgs e)
    {
        if (!e.IsSuccessful)
            return string.Format(Strings.Update_ManualFailed, e.Exception?.GetPrimaryException().Message);

        return AssemblyHelper.CompareVersion(e.LatestVersion!) switch
        {
            > 0 => Strings.Update_ManualAvailable,
            0 => Strings.Update_ManualUpToDate,
            -1 => string.Format(Strings.Update_ManualPreRelease, e.LatestVersion),
            _ => string.Format(Strings.Update_ManualUnparsable, e.LatestVersion),
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
            UpdateStatusText = Strings.Update_StatusFailed;
            UpdateStatus = UpdateCheckStatus.Error;
            UpdateLinkTarget = null;
            return;
        }

        switch (AssemblyHelper.CompareVersion(e.LatestVersion!))
        {
            case > 0:
                UpdateStatusText = string.Format(Strings.Update_StatusAvailable, e.LatestVersion);
                UpdateStatus = UpdateCheckStatus.Available;
                UpdateLinkTarget = e.ReleaseUrl;
                break;
            case 0:
                UpdateStatusText = string.Format(Strings.Update_StatusUpToDate, e.LatestVersion);
                UpdateStatus = UpdateCheckStatus.UpToDate;
                UpdateLinkTarget = null;
                break;
            case -1:
                UpdateStatusText = string.Format(Strings.Update_StatusPreRelease, AssemblyHelper.GetApplicationVersion());
                UpdateStatus = UpdateCheckStatus.PreRelease;
                UpdateLinkTarget = null;
                break;
            default:
                UpdateStatusText = string.Format(Strings.Update_StatusUnparsable, e.LatestVersion);
                UpdateStatus = UpdateCheckStatus.Error;
                UpdateLinkTarget = e.ReleaseUrl;
                break;
        }
    }

    private void OnServerPreferencesSaved(object? sender, List<ServerPreferences> profiles)
        => RunOnUi(RefreshProfiles);

    // Global player defaults changed (Manage Players Save): re-resolve every RUNNING server's roles from its saved
    // profile + the new defaults and regenerate its list files — but only when the resolved assignments actually
    // differ from what it runs with, so unrelated user-preference saves (e.g. LastActiveProfile) are no-ops and
    // a second window reacting to the same save finds nothing left to do.
    // Changes made to a running server's list files during play win (they are newer than the profile), so the
    // roles the server adopted become the profile's roles: saved to that profile, and shown if it is this window's.
    private void OnPlayerRolesAdopted(object? sender, PlayerRolesAdoptedEventArgs e) => RunOnUi(() =>
    {
        var prefs = _serverPrefs.LoadPreferences(e.ProfileName) ?? new ServerPreferences { ProfileName = e.ProfileName };
        prefs.PlayerRoles.Clear();
        foreach (var (key, entry) in e.Result.Overrides) prefs.PlayerRoles[key] = entry;
        _serverPrefs.SavePreferences(prefs);

        if (string.Equals(CurrentProfile?.ProfileName, e.ProfileName, StringComparison.OrdinalIgnoreCase))
            Form.ReplaceRoles(e.Result.Overrides);

        _logger.Information("Saved {count} player role change(s) made in the list files during play to profile '{profile}'.",
            e.Result.LogLines.Count, e.ProfileName);
    });

    private void OnUserPreferencesSaved(object? sender, UserPreferences userPrefs)
        => RunOnUi(() => ApplyDefaultsToRunningServers(userPrefs));

    private void ApplyDefaultsToRunningServers(UserPreferences userPrefs)
    {
        foreach (var profile in _serverPrefs.LoadPreferences())
        {
            if (!_serverManager.TryGet(profile.ProfileName, out var server)) continue;
            if (server.Status == ServerStatus.Stopped) continue;

            var assignments = PlayerRoleResolver.BuildAssignments(profile.PlayerRoles, userPrefs.PlayerDefaults);
            if (assignments.SequenceEqual(server.Options.PlayerRoles)) continue;

            try
            {
                server.ApplyPlayerRoles(
                    new Dictionary<string, PlayerRoleEntry>(profile.PlayerRoles),
                    new Dictionary<string, PlayerDefaultEntry>(userPrefs.PlayerDefaults),
                    server.Options.UsePermittedList);
            }
            catch (Exception ex)
            {
                _logger.Error("Player defaults saved, but regenerating access lists for profile '{profile}' failed: {message}",
                    profile.ProfileName, ex.Message);
            }
        }
    }

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
        Form.PlayerRolesEdited -= OnPlayerRolesEdited;
        _updateProvider.UpdateCheckStarted -= OnUpdateCheckStarted;
        _updateProvider.UpdateCheckFinished -= OnUpdateCheckFinished;
        _serverPrefs.PreferencesSaved -= OnServerPreferencesSaved;
        _userPrefs.PreferencesSaved -= OnUserPreferencesSaved;
        _serverManager.PlayerRolesAdopted -= OnPlayerRolesAdopted;
        Details.Dispose();
        Players.Dispose();
        Logs.Dispose();
    }
}
