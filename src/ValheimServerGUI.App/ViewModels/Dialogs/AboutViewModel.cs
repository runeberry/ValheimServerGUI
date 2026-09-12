using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.App.ViewModels.Dialogs;

/// <summary>About dialog: app version (read from the Core assembly) + external links.</summary>
public partial class AboutViewModel : ObservableObject
{
    private readonly IShellLauncher _shell;

    public AboutViewModel(IShellLauncher shell)
    {
        _shell = shell;
        Version = AssemblyHelper.GetApplicationVersion();
    }

    public string ProductName => AppConstants.ProductName;

    public string Version { get; }

    [RelayCommand] private void OpenGitHub() => _shell.OpenWebAddress(AppConstants.UrlGitHub);
    [RelayCommand] private void OpenDiscord() => _shell.OpenWebAddress(AppConstants.UrlDiscord);
    [RelayCommand] private void OpenDonate() => _shell.OpenWebAddress(AppConstants.UrlDonate);
}
