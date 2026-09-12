using Avalonia.Controls;
using ValheimServerGUI.App.ViewModels;

namespace ValheimServerGUI.App.Views;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
    }

    public SplashWindow(SplashViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
