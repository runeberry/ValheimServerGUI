using Avalonia.Controls;
using Avalonia.Interactivity;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.App.ViewModels.Dialogs;

namespace ValheimServerGUI.App.Views.Dialogs;

/// <summary>
/// "Add player by ID" prompt (grant admin/ban/permit to a platform ID). Closes with an
/// <see cref="AddByIdResult"/> on Add, or null on Cancel.
/// </summary>
public partial class AddByIdWindow : DialogWindow
{
    public AddByIdWindow()
    {
        InitializeComponent();
        DataContext = new AddByIdViewModel();
    }

    private AddByIdViewModel Vm => (AddByIdViewModel)DataContext!;

    private void OnOk(object? sender, RoutedEventArgs e) => Close(Vm.BuildResult());

    private void OnCancel(object? sender, RoutedEventArgs e) => Close((AddByIdResult?)null);
}
