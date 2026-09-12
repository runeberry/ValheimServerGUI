using Avalonia.Controls;
using Avalonia.Interactivity;
using ValheimServerGUI.App.ViewModels.Dialogs;

namespace ValheimServerGUI.App.Views.Dialogs;

public partial class BugReportWindow : Window
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
            "Submitting your report…", "Thanks! Your report was submitted.", "Failed to submit the report");
        var dialog = new AsyncOperationWindow(op, Vm.SubmitAsync);
        await dialog.ShowDialog(this);
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close();
}
