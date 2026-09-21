using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ValheimServerGUI.App.ViewModels.Dialogs;

namespace ValheimServerGUI.App.Views.Dialogs;

public partial class AsyncOperationWindow : DialogWindow
{
    private Func<Task>? _operation;

    public AsyncOperationWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    public AsyncOperationWindow(AsyncOperationViewModel viewModel, Func<Task> operation) : this()
    {
        DataContext = viewModel;
        _operation = operation;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is AsyncOperationViewModel vm && _operation is not null)
            await vm.RunAsync(_operation);
    }

    private void OnClose(object? sender, RoutedEventArgs e) => Close();
}
