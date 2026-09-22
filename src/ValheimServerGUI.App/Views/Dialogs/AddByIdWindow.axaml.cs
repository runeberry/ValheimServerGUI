using Avalonia.Controls;
using Avalonia.Interactivity;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.App.ViewModels.Dialogs;

namespace ValheimServerGUI.App.Views.Dialogs;

/// <summary>
/// "Add player by ID" prompt (grant a single role to a platform ID). The role options offered depend on the
/// active profile's mode (<paramref name="usePermittedList"/>). Closes with an <see cref="AddByIdResult"/> on
/// Add, or null on Cancel.
/// </summary>
public partial class AddByIdWindow : DialogWindow
{
    // Parameterless ctor for the Avalonia runtime loader / designer; the real entry point is the overload below.
    public AddByIdWindow()
    {
        InitializeComponent();
        DataContext = new AddByIdViewModel(usePermittedList: false);
    }

    public AddByIdWindow(bool usePermittedList) : this()
    {
        DataContext = new AddByIdViewModel(usePermittedList);
    }

    private AddByIdViewModel Vm => (AddByIdViewModel)DataContext!;

    private void OnOk(object? sender, RoutedEventArgs e) => Close(Vm.BuildResult());

    private void OnCancel(object? sender, RoutedEventArgs e) => Close((AddByIdResult?)null);
}
