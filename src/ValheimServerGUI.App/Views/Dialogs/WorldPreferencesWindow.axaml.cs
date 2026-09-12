using Avalonia.Controls;
using Avalonia.Interactivity;
using ValheimServerGUI.App.ViewModels.Dialogs;

namespace ValheimServerGUI.App.Views.Dialogs;

public partial class WorldPreferencesWindow : Window
{
    private bool _confirmedClose;

    public WorldPreferencesWindow()
    {
        InitializeComponent();
        Closing += OnClosingGuard;
    }

    public WorldPreferencesWindow(WorldPreferencesViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private WorldPreferencesViewModel Vm => (WorldPreferencesViewModel)DataContext!;

    private void OnOk(object? sender, RoutedEventArgs e)
    {
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
        switch (await DialogGuards.ConfirmSaveDiscardCancelAsync(this))
        {
            case UnsavedChangesChoice.Save:
                Vm.Save();
                _confirmedClose = true;
                Close(true);
                break;
            case UnsavedChangesChoice.Discard:
                _confirmedClose = true;
                Close(false);
                break;
            // Cancel: stay open.
        }
    }
}
