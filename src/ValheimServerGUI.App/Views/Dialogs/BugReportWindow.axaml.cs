using Avalonia.Controls;
using Avalonia.Interactivity;
using ValheimServerGUI.App.ViewModels.Dialogs;

namespace ValheimServerGUI.App.Views.Dialogs;

public partial class BugReportWindow : DialogWindow
{
    public BugReportWindow()
    {
        InitializeComponent();
    }

    public BugReportWindow(BugReportViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private BugReportViewModel Vm => (BugReportViewModel)DataContext!;

    private async void OnSubmit(object? sender, RoutedEventArgs e)
    {
        var op = new AsyncOperationViewModel(
            "Submitting bug report...",
            "Bug report submitted. Thank you!",
            "Failed to submit bug report.\nContact Runeberry Software for further support.");
        var dialog = new AsyncOperationWindow(op, Vm.SubmitAsync);
        await dialog.ShowDialog(this);
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close();
}
