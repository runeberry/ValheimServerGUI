using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ValheimServerGUI.App.Controls;
using ValheimServerGUI.Game;
using ValheimServerGUI.Localization;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.App.ViewModels.Dialogs;

/// <summary>
/// One choice in the Set Directories dialog's "Set directories for:" list: every profile (the app-wide paths), or
/// one profile (its overrides).
/// </summary>
public sealed record DirectoryTarget(string? ProfileName)
{
    public static readonly DirectoryTarget AllProfiles = new((string?)null);

    public bool IsAllProfiles => ProfileName is null;

    public override string ToString() => ProfileName ?? Strings.Directories_Target_AllProfiles;
}

/// <summary>
/// Set Directories dialog (§10.1): the app-wide server binary, save data and (Linux) BepInEx paths, or one profile's
/// overrides of them, picked from "Set directories for:". An empty override uses the app-wide value. Edits are kept
/// per target while switching; OK saves every changed target, Cancel none. A running profile's overrides are
/// read-only. Restore Defaults resets the app-wide paths to the OS defaults, or clears a profile's overrides.
/// </summary>
public partial class DirectoriesViewModel : ModalEditViewModel
{
    private readonly IUserPreferencesProvider _userPrefs;
    private readonly IServerPreferencesProvider _serverPrefs;
    private readonly IValheimPathResolver _pathResolver;
    private readonly IShellLauncher _shell;
    private readonly Func<string, bool> _isProfileRunning;

    // Loaded and edited values per visited target; Loaded stays as read, for change detection on save.
    private readonly Dictionary<DirectoryTarget, (DirectoryPaths Loaded, DirectoryPaths Edited)> _targets = new();

    public DirectoriesViewModel(
        IUserPreferencesProvider userPrefs,
        IServerPreferencesProvider serverPrefs,
        IValheimPathResolver pathResolver,
        IShellLauncher shell,
        string? profileName = null,
        Func<string, bool>? isProfileRunning = null)
    {
        _userPrefs = userPrefs;
        _serverPrefs = serverPrefs;
        _pathResolver = pathResolver;
        _shell = shell;
        _isProfileRunning = isProfileRunning ?? (_ => false);

        var profiles = _serverPrefs.LoadPreferences()
            .Select(p => p.ProfileName)
            .OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase)
            .Select(n => new DirectoryTarget(n));
        Targets = new object[] { DirectoryTarget.AllProfiles, DropdownSeparator.Instance }.Concat(profiles).ToList();

        IgnoreForDirty(nameof(SelectedTarget), nameof(Placeholder), nameof(IsInputEnabled));
        _selectedTarget = Targets.OfType<DirectoryTarget>().FirstOrDefault(t => t.ProfileName == profileName)
            ?? DirectoryTarget.AllProfiles;
        LoadClean(() => ShowTarget(_selectedTarget));
    }

    /// <summary>"All profiles", a separator, then every profile.</summary>
    public IReadOnlyList<object> Targets { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Placeholder), nameof(IsInputEnabled))]
    private DirectoryTarget _selectedTarget;

    [ObservableProperty] private string _serverExePath = string.Empty;
    [ObservableProperty] private string _saveDataFolderPath = string.Empty;
    [ObservableProperty] private string _bepInExPreloaderPath = string.Empty;
    [ObservableProperty] private string _doorstopLibraryPath = string.Empty;

    /// <summary>The BepInEx paths only matter on Linux (on Windows the pack's own winhttp.dll loads it).</summary>
    public bool ShowsModPaths { get; } = !OperatingSystem.IsWindows();

    /// <summary>Shown in an empty field: a profile's empty override uses the app-wide value.</summary>
    public string? Placeholder => SelectedTarget.IsAllProfiles ? null : Strings.Directories_UseDefault;

    /// <summary>False while the selected profile's server runs (its paths only change while it is stopped).</summary>
    public bool IsInputEnabled => SelectedTarget.IsAllProfiles || !_isProfileRunning(SelectedTarget.ProfileName!);

    partial void OnSelectedTargetChanging(DirectoryTarget value) => StoreEdits();

    partial void OnSelectedTargetChanged(DirectoryTarget value) => WithoutDirtying(() => ShowTarget(value));

    // An empty override opens the app-wide path it falls back to. OpenDirectory accepts a file too (opens its folder).
    [RelayCommand]
    private void OpenServerExeFolder() => _shell.OpenDirectory(Effective(ServerExePath, p => p.ServerExe));

    [RelayCommand]
    private void OpenSaveFolder() => _shell.OpenDirectory(Effective(SaveDataFolderPath, p => p.SaveData));

    [RelayCommand]
    private void OpenBepInExPreloaderFolder() => _shell.OpenDirectory(Effective(BepInExPreloaderPath, p => p.BepInExPreloader));

    [RelayCommand]
    private void OpenDoorstopLibraryFolder() => _shell.OpenDirectory(Effective(DoorstopLibraryPath, p => p.DoorstopLibrary));

    private string Effective(string value, Func<DirectoryPaths, string> field)
    {
        if (!string.IsNullOrWhiteSpace(value) || SelectedTarget.IsAllProfiles) return value;
        StoreEdits();
        return field(_targets.TryGetValue(DirectoryTarget.AllProfiles, out var all) ? all.Edited : LoadPaths(DirectoryTarget.AllProfiles));
    }

    /// <summary>True when a changed path does not exist on disk (drives the "Save anyway?" prompt).</summary>
    public bool HasMissingPath => MissingPathDescription is not null;

    /// <summary>
    /// A message naming the first missing path among the changed targets, or null if they all exist. The server
    /// binary and save data are checked whenever their target changed; the BepInEx paths only when they themselves
    /// changed, since an unmodded server simply has none.
    /// </summary>
    public string? MissingPathDescription
    {
        get
        {
            StoreEdits();
            foreach (var (_, (loaded, edited)) in _targets.Where(t => t.Value.Loaded != t.Value.Edited))
            {
                if (IsMissingFile(edited.ServerExe)) return string.Format(Strings.Directories_MissingServerExe, edited.ServerExe);
                if (IsMissingFolder(edited.SaveData)) return string.Format(Strings.Directories_MissingSaveData, edited.SaveData);
                if (!ShowsModPaths) continue;
                if (edited.BepInExPreloader != loaded.BepInExPreloader && IsMissingFile(edited.BepInExPreloader))
                    return string.Format(Strings.Directories_MissingBepInExPreloader, edited.BepInExPreloader);
                if (edited.DoorstopLibrary != loaded.DoorstopLibrary && IsMissingFile(edited.DoorstopLibrary))
                    return string.Format(Strings.Directories_MissingDoorstopLibrary, edited.DoorstopLibrary);
            }
            return null;
        }
    }

    private static bool IsMissingFile(string path) => !string.IsNullOrWhiteSpace(path) && !File.Exists(path);

    private static bool IsMissingFolder(string path) => !string.IsNullOrWhiteSpace(path) && !Directory.Exists(path);

    public override void ApplyDefaults()
    {
        if (SelectedTarget.IsAllProfiles)
        {
            ServerExePath = _pathResolver.DefaultServerPath;
            SaveDataFolderPath = _pathResolver.DefaultSaveDataFolder;
            BepInExPreloaderPath = _pathResolver.DefaultBepInExPreloaderPath;
            DoorstopLibraryPath = _pathResolver.DefaultDoorstopLibraryPath;
        }
        else
        {
            ServerExePath = SaveDataFolderPath = BepInExPreloaderPath = DoorstopLibraryPath = string.Empty;
        }
    }

    /// <summary>Saves every target whose paths changed: the app-wide paths first, then each profile's overrides.</summary>
    public void Save()
    {
        StoreEdits();
        var changed = _targets.Where(t => t.Value.Loaded != t.Value.Edited).ToList();

        foreach (var (target, (_, paths)) in changed.Where(t => t.Key.IsAllProfiles))
        {
            var prefs = _userPrefs.LoadPreferences();
            prefs.ServerExePath = paths.ServerExe;
            prefs.SaveDataFolderPath = paths.SaveData;
            prefs.BepInExPreloaderPath = paths.BepInExPreloader;
            prefs.DoorstopLibraryPath = paths.DoorstopLibrary;
            _userPrefs.SavePreferences(prefs);
        }

        foreach (var (target, (_, paths)) in changed.Where(t => !t.Key.IsAllProfiles))
        {
            var prefs = _serverPrefs.LoadPreferences(target.ProfileName!);
            if (prefs is null) continue; // removed while the dialog was open
            prefs.ServerExePath = NullIfBlank(paths.ServerExe);
            prefs.SaveDataFolderPath = NullIfBlank(paths.SaveData);
            prefs.BepInExPreloaderPath = NullIfBlank(paths.BepInExPreloader);
            prefs.DoorstopLibraryPath = NullIfBlank(paths.DoorstopLibrary);
            _serverPrefs.SavePreferences(prefs);
        }
    }

    private static string? NullIfBlank(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private void StoreEdits()
    {
        if (_targets.TryGetValue(SelectedTarget, out var entry))
            _targets[SelectedTarget] = (entry.Loaded, new DirectoryPaths(ServerExePath, SaveDataFolderPath, BepInExPreloaderPath, DoorstopLibraryPath));
    }

    private void ShowTarget(DirectoryTarget target)
    {
        if (!_targets.TryGetValue(target, out var entry))
        {
            var loaded = LoadPaths(target);
            entry = (loaded, loaded);
            _targets[target] = entry;
        }

        ServerExePath = entry.Edited.ServerExe;
        SaveDataFolderPath = entry.Edited.SaveData;
        BepInExPreloaderPath = entry.Edited.BepInExPreloader;
        DoorstopLibraryPath = entry.Edited.DoorstopLibrary;
    }

    private DirectoryPaths LoadPaths(DirectoryTarget target)
    {
        if (target.IsAllProfiles)
        {
            var p = _userPrefs.LoadPreferences();
            return new DirectoryPaths(p.ServerExePath, p.SaveDataFolderPath, p.BepInExPreloaderPath, p.DoorstopLibraryPath);
        }

        var s = _serverPrefs.LoadPreferences(target.ProfileName!);
        return new DirectoryPaths(s?.ServerExePath ?? string.Empty, s?.SaveDataFolderPath ?? string.Empty,
            s?.BepInExPreloaderPath ?? string.Empty, s?.DoorstopLibraryPath ?? string.Empty);
    }

    private sealed record DirectoryPaths(string ServerExe, string SaveData, string BepInExPreloader, string DoorstopLibrary);
}
