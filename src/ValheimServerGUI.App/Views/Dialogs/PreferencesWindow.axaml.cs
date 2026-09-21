using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ValheimServerGUI.App.ViewModels.Dialogs;

namespace ValheimServerGUI.App.Views.Dialogs;

public partial class PreferencesWindow : DialogWindow
{
    private bool _confirmedClose;

    public PreferencesWindow()
    {
        InitializeComponent();
        Closing += OnClosingGuard;
    }

    public PreferencesWindow(PreferencesViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private PreferencesViewModel Vm => (PreferencesViewModel)DataContext!;

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        Vm.Save();
        App.Instance.ApplyTheme(Vm.SavedTheme);
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
