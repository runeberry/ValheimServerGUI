using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ValheimServerGUI.App.Views.Tabs;

public partial class ServerDetailsView : UserControl
{
    public ServerDetailsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
