using Avalonia.Controls;
using Avalonia.Interactivity;
using ValheimServerGUI.App.ViewModels.Dialogs;
using ValheimServerGUI.Localization;

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
            Strings.BugReport_Submitting,
            Strings.BugReport_Submitted,
            Strings.BugReport_Failed);
        var dialog = new AsyncOperationWindow(op, Vm.SubmitAsync);
        await dialog.ShowDialog(this);
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close();
}
