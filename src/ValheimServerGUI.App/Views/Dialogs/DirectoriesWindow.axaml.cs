using Avalonia.Controls;
using Avalonia.Interactivity;
using ValheimServerGUI.App.ViewModels.Dialogs;

namespace ValheimServerGUI.App.Views.Dialogs;

public partial class DirectoriesWindow : DialogWindow
{
    private bool _confirmedClose;

    public DirectoriesWindow()
    {
        InitializeComponent();
        Closing += OnClosingGuard;
    }

    public DirectoriesWindow(DirectoriesViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private DirectoriesViewModel Vm => (DirectoriesViewModel)DataContext!;

    // Path browsing now lives inside FilenameFormField (its built-in browse button).

    private async void OnOk(object? sender, RoutedEventArgs e)
    {
        if (Vm.MissingPathDescription is { } missing)
        {
            var proceed = await MessageBox.ConfirmAsync(this, "Path not found", $"{missing}\n\nSave anyway?");
            if (!proceed) return;
        }

        Vm.Save();
        _confirmedClose = true;
        Close(true);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);

    private void OnRestoreDefaults(object? sender, RoutedEventArgs e) => Vm.ApplyDefaults();

    private async void OnClosingGuard(object? sender, WindowClosingEventArgs e)
    {
        if (_confirmedClose || !Vm.IsDirty) return;

        e.Cancel = true;
        if (await DialogGuards.ConfirmDiscardAsync(this))
        {
            _confirmedClose = true;
            Close(false);
        }
    }
}
