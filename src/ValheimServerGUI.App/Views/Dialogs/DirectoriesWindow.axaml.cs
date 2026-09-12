using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
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

    private async void BrowseExe(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select the Valheim dedicated server executable",
            AllowMultiple = false,
        });
        var path = files.FirstOrDefault()?.TryGetLocalPath();
        if (!string.IsNullOrEmpty(path)) Vm.ServerExePath = path;
    }

    private async void BrowseFolder(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select the save data folder",
            AllowMultiple = false,
        });
        var path = folders.FirstOrDefault()?.TryGetLocalPath();
        if (!string.IsNullOrEmpty(path)) Vm.SaveDataFolderPath = path;
    }

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
