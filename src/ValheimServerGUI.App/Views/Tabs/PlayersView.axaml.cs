using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ValheimServerGUI.App.Views.Tabs;

public partial class PlayersView : UserControl
{
    public PlayersView()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
