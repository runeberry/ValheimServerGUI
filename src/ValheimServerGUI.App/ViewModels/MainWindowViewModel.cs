using CommunityToolkit.Mvvm.ComponentModel;
using ValheimServerGUI.Game;

namespace ValheimServerGUI.App.ViewModels;

/// <summary>
/// One server window's view-model. Each window owns a transient <see cref="ValheimServer"/> (resolved
/// per-window via a factory) over the shared singleton providers (§2.2). This wave establishes the
/// per-window identity — the profile it is bound to — and the ownership/teardown of the server; the full
/// §10.5 observable surface (status, commands, tabs) is layered on in later waves.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ValheimServer _server;
    private readonly IUserPreferencesProvider _userPrefs;

    public MainWindowViewModel(
        ValheimServer server,
        IUserPreferencesProvider userPrefs)
    {
        _server = server;
        _userPrefs = userPrefs;
    }

    /// <summary>The per-window server controller (transient — one instance per window).</summary>
    public ValheimServer Server => _server;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    private ServerPreferences? _currentProfile;

    /// <summary>Set by <see cref="StartupProfileSelector"/> to auto-start this window's server at launch (§2.3).</summary>
    public bool AutoStartOnLoad { get; set; }

    public string Title => CurrentProfile is null
        ? AppConstants.ProductName
        : $"{AppConstants.ProductName} — {CurrentProfile.ProfileName}";

    /// <summary>
    /// Binds a profile to this window and records it as the last-active profile (§16.2) so relaunch
    /// reopens here. Only writes when the value actually changes, to avoid a save storm.
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

    protected override void DisposeCore()
    {
        _server.Dispose();
    }
}
