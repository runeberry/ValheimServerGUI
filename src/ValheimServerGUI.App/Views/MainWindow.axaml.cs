using Avalonia.Controls;
using ValheimServerGUI.App.ViewModels;

namespace ValheimServerGUI.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel viewModel) : this()
    {
        ViewModel = viewModel;
        DataContext = viewModel;
    }

    /// <summary>The per-window view-model (each window owns its own).</summary>
    public MainWindowViewModel? ViewModel { get; }
}
