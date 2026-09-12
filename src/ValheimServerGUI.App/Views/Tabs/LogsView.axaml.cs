using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.App.Views.Dialogs;

namespace ValheimServerGUI.App.Views.Tabs;

public partial class LogsView : UserControl
{
    private LogsViewModel? _wired;

    public LogsView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Wire();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void Wire()
    {
        if (_wired is not null)
        {
            _wired.SaveLogsRequested -= OnSaveLogsRequested;
            _wired.Warning -= OnWarning;
            _wired = null;
        }

        if ((DataContext as MainWindowViewModel)?.Logs is { } logs)
        {
            logs.SaveLogsRequested += OnSaveLogsRequested;
            logs.Warning += OnWarning;
            _wired = logs;
        }
    }

    private async Task OnSaveLogsRequested(string viewName, IReadOnlyList<string> lines)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Logs",
            SuggestedFileName = $"{viewName}Logs.txt",
            DefaultExtension = "txt",
        });

        if (file is null) return;

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream);
        foreach (var line in lines)
            await writer.WriteLineAsync(line);
    }

    private async void OnWarning(string message)
        => await new MessageWindow("Save Logs", message).ShowDialog(GetWindow());

    private Window GetWindow() => (Window)TopLevel.GetTopLevel(this)!;
}
