using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Logging;

namespace ValheimServerGUI.App.ViewModels;

/// <summary>
/// Logs tab (§10.2): a view selector (Server / Application, default Server) over two per-view buffers.
/// Server lines arrive through <see cref="AppendServerLine"/> (wired as the server's
/// <c>options.LogMessageHandler</c>); application lines from the app logger's buffer + <c>LogReceived</c>.
/// Clear/Save act on the current view only; Save warns when the view is empty.
/// </summary>
public partial class LogsViewModel : ViewModelBase
{
    private readonly IApplicationLogger _appLogger;
    private readonly IShellLauncher _shell;
    private readonly IValheimPathResolver _pathResolver;

    public LogsViewModel(IApplicationLogger appLogger, IShellLauncher shell, IValheimPathResolver pathResolver)
    {
        _appLogger = appLogger;
        _shell = shell;
        _pathResolver = pathResolver;

        foreach (var line in _appLogger.LogBuffer) AppLines.Add(line);
        _appLogger.LogReceived += OnAppLogReceived;
    }

    public IReadOnlyList<string> Views { get; } = new[] { LogViews.Server, LogViews.Application };

    public ObservableCollection<string> ServerLines { get; } = new();
    public ObservableCollection<string> AppLines { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentLines))]
    private string _selectedView = LogViews.Server;

    public ObservableCollection<string> CurrentLines =>
        SelectedView == LogViews.Application ? AppLines : ServerLines;

    /// <summary>Shows the save-file picker and writes the current view's lines (wired by the window).</summary>
    public event Func<string, IReadOnlyList<string>, Task>? SaveLogsRequested;

    /// <summary>Surfaces a warning (e.g. nothing to save).</summary>
    public event Action<string>? Warning;

    /// <summary>The server log stream handler — passed as <c>ValheimServerOptions.LogMessageHandler</c>.</summary>
    public void AppendServerLine(string line) => RunOnUi(() => ServerLines.Add(line));

    [RelayCommand]
    private void ClearLogs() => CurrentLines.Clear();

    [RelayCommand]
    private void SaveLogs()
    {
        var lines = CurrentLines.ToList();
        if (lines.Count == 0)
        {
            Warning?.Invoke("No logs to save!");
            return;
        }

        _ = SaveLogsRequested?.Invoke(SelectedView, lines) ?? Task.CompletedTask;
    }

    [RelayCommand]
    private void OpenLogsFolder() => _shell.OpenDirectory(_pathResolver.LogsFolderPath);

    private void OnAppLogReceived(string line) => RunOnUi(() => AppLines.Add(line));

    protected override void DisposeCore()
    {
        _appLogger.LogReceived -= OnAppLogReceived;
    }
}
