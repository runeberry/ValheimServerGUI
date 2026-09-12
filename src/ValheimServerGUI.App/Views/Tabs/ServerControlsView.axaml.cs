using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ValheimServerGUI.App.Views.Tabs;

public partial class ServerControlsView : UserControl
{
    public ServerControlsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
