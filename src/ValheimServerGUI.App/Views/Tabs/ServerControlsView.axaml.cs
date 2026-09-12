using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ValheimServerGUI.App.Infrastructure;
using ValheimServerGUI.App.ViewModels;

namespace ValheimServerGUI.App.Views.Tabs;

public partial class ServerControlsView : UserControl
{
    public ServerControlsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private async void CopyPassword(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            await ClipboardHelper.CopyTextAsync(this, vm.Form.Password);
    }
}
