using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ValheimServerGUI.Localization;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.App.ViewModels.Dialogs;

/// <summary>About dialog: product identity, version + build date, legal/attribution text, and external
/// links.</summary>
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

    /// <summary>The About header product name, without the "(Unofficial)" prefix of
    /// <see cref="AppConstants.ProductName"/>.</summary>
    public string ProductName => "Valheim Dedicated Server GUI";

    public string Copyright => "© 2023-2026 Runeberry Software, LLC";

    public string License => Strings.About_License;

    public string Disclaimer => Strings.About_Disclaimer;

    public string Version { get; }

    public string BuildDate { get; }

    [RelayCommand] private void OpenSourceCode() => _shell.OpenWebAddress(AppConstants.UrlSourceCode);
    [RelayCommand] private void OpenDiscord() => _shell.OpenWebAddress(AppConstants.UrlDiscord);
    [RelayCommand] private void OpenTipJar() => _shell.OpenWebAddress(AppConstants.UrlTipJar);
    [RelayCommand] private void OpenValheimSite() => _shell.OpenWebAddress(AppConstants.UrlValheimGameSite);
}
