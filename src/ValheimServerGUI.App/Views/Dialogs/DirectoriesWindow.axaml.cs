using Avalonia.Controls;
using Avalonia.Interactivity;
using ValheimServerGUI.App.ViewModels.Dialogs;

namespace ValheimServerGUI.App.Views.Dialogs;

public partial class DirectoriesWindow : Window
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
        if (Vm.HasMissingPath)
        {
            var proceed = await new ConfirmWindow("Path not found",
                "One or more paths do not exist. Save anyway?").ShowDialog<bool>(this);
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
