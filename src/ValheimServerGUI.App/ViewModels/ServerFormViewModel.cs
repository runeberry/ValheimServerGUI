using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ValheimServerGUI.Game;

namespace ValheimServerGUI.App.ViewModels;

/// <summary>
/// The editable Server Controls + Advanced Controls form state, plus the form↔prefs mappers (the v2.4
/// <c>GetPrefsFromFormState</c>/<c>SetFormStateFromPrefs</c>). Kept a passive data holder: the owning
/// <see cref="MainWindowViewModel"/> drives world listing and the options build (which need the providers).
/// The <c>" (cloud)"</c> suffix is view-only — <see cref="SelectedWorldName"/> strips it so it never reaches
/// prefs/options/validation.
/// </summary>
public partial class ServerFormViewModel : ObservableObject
{
    // Nesting-safe suppression counter for the dirty flag (mirrors Dialogs/ModalEditViewModel). Loads and
    // app-driven mutations run under RunClean so only direct user edits via the field bindings set IsDirty.
    private int _suppressDirty;

    public ServerFormViewModel()
    {
        PropertyChanged += (_, e) =>
        {
            if (_suppressDirty > 0) return;
            // IsDirty itself, and the view-only password-visibility toggle, never count as edits.
            if (e.PropertyName is nameof(IsDirty) or nameof(ShowPassword)) return;
            IsDirty = true;
        };
    }

    /// <summary>True once the user has changed any field since the last load (backs the unsaved-changes guard).</summary>
    [ObservableProperty]
    private bool _isDirty;

    /// <summary>Runs a form mutation without tripping <see cref="IsDirty"/> (loads, app-driven world refresh).</summary>
    public void RunClean(Action mutate)
    {
        _suppressDirty++;
        try { mutate(); }
        finally { _suppressDirty--; }
    }

    // ----- Server Controls -----
    [ObservableProperty] private string? _name;
    [ObservableProperty] private int _port = CoreConstants.DefaultServerPort;
    [ObservableProperty] private string? _password;
    [ObservableProperty] private bool _showPassword;
    [ObservableProperty] private bool _useNewWorld;
    [ObservableProperty] private string? _existingWorld;
    [ObservableProperty] private string? _newWorldName;
    [ObservableProperty] private bool _isPublic;
    [ObservableProperty] private bool _crossplay;

    // ----- Players / access (profile working state) -----
    // The single per-player role map (keyed by PlayerInfo.Key) + the permitted-list mode flag. These are
    // profile working state edited via the Players tab; they trip IsDirty and are persisted with Save, then
    // projected onto the three list files at server start.
    private readonly Dictionary<string, PlayerRoleEntry> _playerRoles = new();

    /// <summary>Permitted-list mode (a form field → trips <see cref="IsDirty"/>; cleared on load via RunClean).</summary>
    [ObservableProperty] private bool _usePermittedList;

    /// <summary>Raised whenever the role map changes (add/remove/replace, or a bulk load), so the Players tab
    /// can re-render. Not raised for the mode flag — that surfaces as an ordinary PropertyChanged.</summary>
    public event EventHandler? RoleStateChanged;

    /// <summary>The stored role for a player key, or null when the player has no role.</summary>
    public PlayerRole? GetRole(string key)
        => _playerRoles.TryGetValue(key, out var entry) ? entry.Role : null;

    /// <summary>
    /// Sets (or clears, when <paramref name="role"/> is null) a player's single role. A real change trips
    /// <see cref="IsDirty"/> (unless under <see cref="RunClean"/>) and raises <see cref="RoleStateChanged"/>.
    /// </summary>
    public void SetRole(PlayerInfo player, PlayerRole? role)
    {
        var key = player.Key;

        if (role is null)
        {
            if (!_playerRoles.Remove(key)) return; // already had no role — no change
        }
        else
        {
            // Preserve the raw platform token so non-Steam file entries keep the game's exact casing.
            var entry = new PlayerRoleEntry(role.Value, player.PlatformRaw ?? player.Platform);
            if (_playerRoles.TryGetValue(key, out var existing) && existing == entry) return; // no change
            _playerRoles[key] = entry;
        }

        if (_suppressDirty == 0) IsDirty = true;
        RoleStateChanged?.Invoke(this, EventArgs.Empty);
    }

    // ----- Advanced Controls -----
    [ObservableProperty] private string? _serverExePath;
    [ObservableProperty] private string? _saveDataFolderPath;
    [ObservableProperty] private bool _autoStart;
    [ObservableProperty] private bool _writeServerLogsToFile = true;
    [ObservableProperty] private string? _additionalArgs;
    [ObservableProperty] private int _saveInterval = CoreConstants.DefaultSaveInterval;
    [ObservableProperty] private int _backupCount = CoreConstants.DefaultBackupCount;
    [ObservableProperty] private int _backupIntervalShort = CoreConstants.DefaultBackupIntervalShort;
    [ObservableProperty] private int _backupIntervalLong = CoreConstants.DefaultBackupIntervalLong;

    /// <summary>Toggles password visibility (the in-field eye button).</summary>
    [RelayCommand]
    private void ToggleShowPassword() => ShowPassword = !ShowPassword;

    /// <summary>Existing worlds to choose from (may include cloud entries with the <c>" (cloud)"</c> suffix).</summary>
    public ObservableCollection<string> Worlds { get; } = new();

    /// <summary>The world name that should reach prefs/options: the new-world name, or the selected existing
    /// world with any view-only cloud suffix stripped.</summary>
    public string? SelectedWorldName => UseNewWorld ? NewWorldName : StripCloudSuffix(ExistingWorld);

    /// <summary>True when an existing, cloud-only world is selected (needs importing before it can be hosted).</summary>
    public bool IsSelectedWorldCloud => !UseNewWorld
        && ExistingWorld is not null
        && ExistingWorld.EndsWith(AppConstants.CloudWorldSuffix, System.StringComparison.Ordinal);

    public static string? StripCloudSuffix(string? world)
        => world is not null && world.EndsWith(AppConstants.CloudWorldSuffix, System.StringComparison.Ordinal)
            ? world[..^AppConstants.CloudWorldSuffix.Length]
            : world;

    /// <summary>SetFormStateFromPrefs (scalar fields only; the owner drives world listing + selection).</summary>
    public void LoadFieldsFrom(ServerPreferences prefs)
    {
        RunClean(() =>
        {
            Name = prefs.Name;
            Port = prefs.Port;
            Password = prefs.Password;
            ShowPassword = false;
            IsPublic = prefs.Public;
            Crossplay = prefs.Crossplay;
            SaveInterval = prefs.SaveInterval;
            BackupCount = prefs.BackupCount;
            BackupIntervalShort = prefs.BackupIntervalShort;
            BackupIntervalLong = prefs.BackupIntervalLong;
            AutoStart = prefs.AutoStart;
            AdditionalArgs = prefs.AdditionalArgs;
            ServerExePath = prefs.ServerExePath;
            SaveDataFolderPath = prefs.SaveDataFolderPath;
            WriteServerLogsToFile = prefs.WriteServerLogsToFile;
            UsePermittedList = prefs.UsePermittedList;

            _playerRoles.Clear();
            foreach (var (key, entry) in prefs.PlayerRoles) _playerRoles[key] = entry;
        });
        IsDirty = false; // a load leaves the form clean
        RoleStateChanged?.Invoke(this, EventArgs.Empty); // let the Players tab re-render for the new profile
    }

    /// <summary>GetPrefsFromFormState: merges the form into the (existing or new) profile prefs.</summary>
    public ServerPreferences ToPreferences(ServerPreferences prefs)
    {
        prefs.Name = Name;
        prefs.Port = Port;
        prefs.Password = Password;
        prefs.WorldName = SelectedWorldName;
        prefs.Public = IsPublic;
        prefs.Crossplay = Crossplay;
        prefs.SaveInterval = SaveInterval;
        prefs.BackupCount = BackupCount;
        prefs.BackupIntervalShort = BackupIntervalShort;
        prefs.BackupIntervalLong = BackupIntervalLong;
        prefs.AutoStart = AutoStart;
        prefs.AdditionalArgs = AdditionalArgs;
        prefs.ServerExePath = ServerExePath;
        prefs.SaveDataFolderPath = SaveDataFolderPath;
        prefs.WriteServerLogsToFile = WriteServerLogsToFile;
        prefs.UsePermittedList = UsePermittedList;
        prefs.PlayerRoles = new Dictionary<string, PlayerRoleEntry>(_playerRoles);
        return prefs;
    }
}
