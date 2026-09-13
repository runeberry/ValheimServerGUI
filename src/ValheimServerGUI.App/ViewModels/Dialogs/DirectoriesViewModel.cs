using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using ValheimServerGUI.Game;

namespace ValheimServerGUI.App.ViewModels.Dialogs;

/// <summary>
/// Set Directories dialog (§10.1): the app-wide default server-exe and save-data paths (the per-profile
/// overrides live on the Advanced tab). Restore-Defaults resets to the OS resolver's defaults; the window
/// asks "Save anyway?" if a path does not exist.
/// </summary>
public partial class DirectoriesViewModel : ModalEditViewModel
{
    private readonly IUserPreferencesProvider _userPrefs;
    private readonly IValheimPathResolver _pathResolver;

    public DirectoriesViewModel(IUserPreferencesProvider userPrefs, IValheimPathResolver pathResolver)
    {
        _userPrefs = userPrefs;
        _pathResolver = pathResolver;
        Load();
    }

    [ObservableProperty] private string _serverExePath = string.Empty;
    [ObservableProperty] private string _saveDataFolderPath = string.Empty;

    /// <summary>True when either path is set but does not exist on disk (drives the "Save anyway?" prompt).</summary>
    public bool HasMissingPath => MissingPathDescription is not null;

    /// <summary>A message naming the first missing path (for the "Save anyway?" prompt), or null if all exist.</summary>
    public string? MissingPathDescription
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(ServerExePath) && !File.Exists(ServerExePath))
                return $"The server executable does not exist:\n{ServerExePath}";
            if (!string.IsNullOrWhiteSpace(SaveDataFolderPath) && !Directory.Exists(SaveDataFolderPath))
                return $"The save data folder does not exist:\n{SaveDataFolderPath}";
            return null;
        }
    }

    private void Load() => LoadClean(() =>
    {
        var p = _userPrefs.LoadPreferences();
        ServerExePath = p.ServerExePath;
        SaveDataFolderPath = p.SaveDataFolderPath;
    });

    public override void ApplyDefaults()
    {
        ServerExePath = _pathResolver.DefaultServerPath;
        SaveDataFolderPath = _pathResolver.DefaultSaveDataFolder;
    }

    public void Save()
    {
        var p = _userPrefs.LoadPreferences();
        p.ServerExePath = ServerExePath;
        p.SaveDataFolderPath = SaveDataFolderPath;
        _userPrefs.SavePreferences(p);
    }
}
