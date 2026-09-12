using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ValheimServerGUI.App.Infrastructure;
using ValheimServerGUI.App.ViewModels;

namespace ValheimServerGUI.App.Views.Tabs;

public partial class ServerDetailsView : UserControl
{
    public ServerDetailsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private ServerDetailsViewModel? Details => (DataContext as MainWindowViewModel)?.Details;

    private async void CopyExternal(object? sender, RoutedEventArgs e) => await Copy(Details?.ExternalIp);
    private async void CopyInternal(object? sender, RoutedEventArgs e) => await Copy(Details?.InternalIp);
    private async void CopyLocal(object? sender, RoutedEventArgs e) => await Copy(Details?.LocalIp);
    private async void CopyInvite(object? sender, RoutedEventArgs e) => await Copy(Details?.InviteCode);

    private async System.Threading.Tasks.Task Copy(string? text) => await ClipboardHelper.CopyTextAsync(this, text);
}
