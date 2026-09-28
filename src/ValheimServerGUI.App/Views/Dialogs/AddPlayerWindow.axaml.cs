using Avalonia.Interactivity;
using ValheimServerGUI.App.ViewModels.Dialogs;

namespace ValheimServerGUI.App.Views.Dialogs;

/// <summary>
/// Add Player prompt (platform + ID, optional name, and a role from the caller's <see cref="AddPlayerOptions"/>).
/// Closes with an <see cref="AddPlayerResult"/> on Add Player, or null on Cancel.
/// </summary>
public partial class AddPlayerWindow : DialogWindow
{
    // Parameterless ctor for the Avalonia runtime loader / designer; the real entry point is the overload below.
    public AddPlayerWindow()
    {
        InitializeComponent();
        DataContext = new AddPlayerViewModel(AddPlayerOptions.ForServer);
    }

    public AddPlayerWindow(AddPlayerOptions options) : this()
    {
        DataContext = new AddPlayerViewModel(options);
    }

    private AddPlayerViewModel Vm => (AddPlayerViewModel)DataContext!;

    private void OnOk(object? sender, RoutedEventArgs e) => Close(Vm.BuildResult());

    private void OnCancel(object? sender, RoutedEventArgs e) => Close((AddPlayerResult?)null);
}
