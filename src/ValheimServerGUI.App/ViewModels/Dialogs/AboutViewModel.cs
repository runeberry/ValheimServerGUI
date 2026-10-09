using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ValheimServerGUI.Localization;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.App.ViewModels.Dialogs;

/// <summary>About dialog: product identity, version + build date, legal/attribution text, and external links
/// (matches the WinForms About form).</summary>
public partial class AboutViewModel : ObservableObject
{
    private readonly IShellLauncher _shell;

    public AboutViewModel(IShellLauncher shell)
    {
        _shell = shell;
        Version = AssemblyHelper.GetApplicationVersion();
        try
        {
            BuildDate = AssemblyHelper.GetApplicationBuildDate().ToUniversalTime().ToDisplayISOFormat();
        }
        catch
        {
            BuildDate = string.Empty;
        }
    }

    /// <summary>The About header product name (no "(Unofficial)" prefix, matching the WinForms About form).</summary>
    public string ProductName => "Valheim Dedicated Server GUI";

    public string Copyright => "© 2023 Runeberry Software, LLC";

    public string License => Strings.About_License;

    public string Disclaimer => Strings.About_Disclaimer;

    public string Version { get; }

    public string BuildDate { get; }

    [RelayCommand] private void OpenGitHub() => _shell.OpenWebAddress(AppConstants.UrlGitHub);
    [RelayCommand] private void OpenDiscord() => _shell.OpenWebAddress(AppConstants.UrlDiscord);
    [RelayCommand] private void OpenDonate() => _shell.OpenWebAddress(AppConstants.UrlDonate);
    [RelayCommand] private void OpenValheimSite() => _shell.OpenWebAddress(AppConstants.UrlValheimGameSite);
}
