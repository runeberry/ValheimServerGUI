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
/// Logs tab (§10.2): a view selector (Server / Application, default Server) over two buffers. The Server
/// buffer is owned by the selected profile's server entry (re-pointed on switch via <see cref="SetServerLog"/>),
/// so server lines land there regardless of which window started the server; application lines come from the
/// app logger's buffer + <c>LogReceived</c>. Clear/Save act on the current view only; Save warns when empty.
/// </summary>
public partial class LogsViewModel : ViewModelBase
{
    private readonly IApplicationLogger _appLogger;
    private readonly IShellLauncher _shell;
    private readonly IValheimPathResolver _pathResolver;

    /// <summary>
    /// Cap on the lines kept per view. Bounds memory and render cost — the log surface is a single contiguous
    /// string, so without a cap it would grow forever. Older lines fall off the top; the full history is still
    /// what "Write server logs to file" persists to disk, and Save Logs writes whatever is currently buffered.
    /// </summary>
    private const int MaxLines = 5000;

    public LogsViewModel(IApplicationLogger appLogger, IShellLauncher shell, IValheimPathResolver pathResolver)
    {
        _appLogger = appLogger;
        _shell = shell;
        _pathResolver = pathResolver;

        foreach (var line in _appLogger.LogBuffer) Append(AppLines, line);
        _appLogger.LogReceived += OnAppLogReceived;
    }

    private static void Append(ObservableCollection<string> lines, string line)
    {
        lines.Add(line);
        while (lines.Count > MaxLines) lines.RemoveAt(0);
    }

    public IReadOnlyList<string> Views { get; } = new[] { LogViews.Server, LogViews.Application };

    // The Server buffer is owned by the selected profile's server entry (IServerManager); SetServerLog
    // re-points it on a switch. Starts as an empty local buffer until the first re-target.
    private ObservableCollection<string> _serverLines = new();
    public ObservableCollection<string> AppLines { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentLines))]
    private string _selectedView = LogViews.Server;

    public ObservableCollection<string> CurrentLines =>
        SelectedView == LogViews.Application ? AppLines : _serverLines;

    /// <summary>Shows the save-file picker and writes the current view's lines (wired by the window).</summary>
    public event Func<string, IReadOnlyList<string>, Task>? SaveLogsRequested;

    /// <summary>Surfaces a warning (e.g. nothing to save).</summary>
    public event Action<string>? Warning;

    /// <summary>Points the Server view at the selected profile's server-owned buffer (called on profile switch).</summary>
    public void SetServerLog(ObservableCollection<string> buffer)
    {
        _serverLines = buffer;
        OnPropertyChanged(nameof(CurrentLines));
    }

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

    private void OnAppLogReceived(string line) => RunOnUi(() => Append(AppLines, line));

    protected override void DisposeCore()
    {
        _appLogger.LogReceived -= OnAppLogReceived;
    }
}
