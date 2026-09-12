using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.App.ViewModels.Dialogs;

/// <summary>
/// Preferences dialog (§10.1): the six app-wide booleans plus the theme (§16.2 enhancement). Applies the
/// run-on-startup registration through <see cref="IStartupManager"/> and re-applies the theme on save.
/// </summary>
public partial class PreferencesViewModel : ModalEditViewModel
{
    private readonly IUserPreferencesProvider _userPrefs;
    private readonly IStartupManager _startupManager;

    public PreferencesViewModel(IUserPreferencesProvider userPrefs, IStartupManager startupManager)
    {
        _userPrefs = userPrefs;
        _startupManager = startupManager;
        Load();
    }

    [ObservableProperty] private bool _checkForUpdates;
    [ObservableProperty] private bool _startWithWindows;
    [ObservableProperty] private bool _startMinimized;
    [ObservableProperty] private bool _saveProfileOnStart;
    [ObservableProperty] private bool _writeApplicationLogsToFile;
    [ObservableProperty] private bool _enablePasswordValidation;
    [ObservableProperty] private AppTheme _theme;

    public IReadOnlyList<AppTheme> Themes { get; } = new[] { AppTheme.System, AppTheme.Light, AppTheme.Dark };

    /// <summary>Set on save so the caller can re-apply the theme to the running app.</summary>
    public AppTheme SavedTheme { get; private set; }

    private void Load() => LoadClean(() =>
    {
        var p = _userPrefs.LoadPreferences();
        CheckForUpdates = p.CheckForUpdates;
        StartWithWindows = p.StartWithWindows;
        StartMinimized = p.StartMinimized;
        SaveProfileOnStart = p.SaveProfileOnStart;
        WriteApplicationLogsToFile = p.WriteApplicationLogsToFile;
        EnablePasswordValidation = p.EnablePasswordValidation;
        Theme = p.Theme;
    });

    public override void ApplyDefaults()
    {
        var d = UserPreferences.GetDefault();
        CheckForUpdates = d.CheckForUpdates;
        StartWithWindows = d.StartWithWindows;
        StartMinimized = d.StartMinimized;
        SaveProfileOnStart = d.SaveProfileOnStart;
        WriteApplicationLogsToFile = d.WriteApplicationLogsToFile;
        EnablePasswordValidation = d.EnablePasswordValidation;
        Theme = d.Theme;
    }

    /// <summary>Flush to the store (OK). Applies the startup registration; theme is re-applied by the caller.</summary>
    public void Save()
    {
        var p = _userPrefs.LoadPreferences();
        p.CheckForUpdates = CheckForUpdates;
        p.StartWithWindows = StartWithWindows;
        p.StartMinimized = StartMinimized;
        p.SaveProfileOnStart = SaveProfileOnStart;
        p.WriteApplicationLogsToFile = WriteApplicationLogsToFile;
        p.EnablePasswordValidation = EnablePasswordValidation;
        p.Theme = Theme;
        _userPrefs.SavePreferences(p);

        _startupManager.ApplyStartupSetting(StartWithWindows);
        SavedTheme = Theme;
    }
}
