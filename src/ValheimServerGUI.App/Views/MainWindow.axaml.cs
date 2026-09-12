using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using ValheimServerGUI.App.Infrastructure;
using ValheimServerGUI.App.Startup;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.App.Views.Dialogs;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.App.Views;

public partial class MainWindow : Window
{
    private bool _forceClose;
    private bool _awaitingStop;
    private TrayIcon? _trayIcon;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel viewModel) : this()
    {
        ViewModel = viewModel;
        DataContext = viewModel;

        viewModel.NewWindowRequested += OnNewWindowRequested;
        viewModel.CloseRequested += Close;
        viewModel.CloudImportPrompt = ShowCloudImportAsync;
        viewModel.ErrorReported = msg => _ = ShowMessageAsync("Error starting server", msg);
        viewModel.StopTimedOutWarning += () =>
            _ = ShowMessageAsync("Server force-stopped",
                "The server did not shut down in time and was force-stopped. Recent world changes may not have been saved.");

        Closing += OnClosing;
        Opened += OnOpened;
        SetUpTrayIcon();
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (ViewModel is { AutoStartOnLoad: true })
            await ViewModel.StartServerAsync(isManual: false);
    }

    // Tab-visible lazy refresh (§10.2): the Details/Players 1s timers run only while their tab is shown.
    private void OnTabChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ViewModel is null) return;
        ViewModel.Details.SetActive(DetailsTab.IsSelected);
        ViewModel.Players.SetActive(PlayersTab.IsSelected);
    }

    private async Task<CloudImportChoice> ShowCloudImportAsync(string worldName)
        => await new CloudImportWindow(worldName).ShowDialog<CloudImportChoice>(this);

    private async Task ShowMessageAsync(string title, string message)
        => await new MessageWindow(title, message).ShowDialog(this);

    /// <summary>The per-window view-model (each window owns its own).</summary>
    public MainWindowViewModel? ViewModel { get; }

    private void OnNewWindowRequested()
        => App.Instance.Services.GetRequiredService<ShellCoordinator>().OpenNewWindow();

    // §2.4 "safe shutdowns": decide via CloseDecider, then perform the Stop/defer/close here.
    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_forceClose || ViewModel is null) return;

        var isOsShutdown = e.CloseReason is WindowCloseReason.OSShutdown or WindowCloseReason.ApplicationShutdown;
        var prompt = App.Instance.Services.GetRequiredService<IUserPrompt>();

        switch (CloseDecider.Decide(ViewModel.Server.Status, isOsShutdown, prompt, ViewModel.Title))
        {
            case CloseDecision.Proceed:
                return;

            case CloseDecision.Cancel:
                e.Cancel = true;
                return;

            case CloseDecision.StopThenClose:
                e.Cancel = true;
                DeferCloseUntilStopped();
                ViewModel.Server.Stop();
                return;
        }
    }

    private void DeferCloseUntilStopped()
    {
        if (_awaitingStop || ViewModel is null) return;
        _awaitingStop = true;

        void OnStatusChanged(object? sender, ServerStatus status)
        {
            if (status != ServerStatus.Stopped) return;
            ViewModel.Server.StatusChanged -= OnStatusChanged;
            Dispatcher.UIThread.Post(() =>
            {
                _forceClose = true;
                Close();
            });
        }

        ViewModel.Server.StatusChanged += OnStatusChanged;
    }

    // Tray icon + native menu (§10.4). Both buttons and tray items bind the SAME VM commands, so their
    // enablement is derived, never duplicated. Only under a desktop lifetime (skipped headless / no tray).
    private void SetUpTrayIcon()
    {
        if (ViewModel is null) return;
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime) return;

        try
        {
            var menu = new NativeMenu();

            var header = new NativeMenuItem { Header = ViewModel.CurrentProfile?.ProfileName ?? "Server" };
            header.Click += (_, _) => RestoreAndActivate();
            menu.Add(header);
            menu.Add(new NativeMenuItemSeparator());
            menu.Add(new NativeMenuItem { Header = "Start", Command = ViewModel.StartCommand });
            menu.Add(new NativeMenuItem { Header = "Restart", Command = ViewModel.RestartCommand });
            menu.Add(new NativeMenuItem { Header = "Stop", Command = ViewModel.StopCommand });
            menu.Add(new NativeMenuItemSeparator());
            menu.Add(new NativeMenuItem { Header = "Close", Command = ViewModel.CloseCommand });

            _trayIcon = new TrayIcon
            {
                Icon = PlaceholderIcon.Create(),
                ToolTipText = ViewModel.Title,
                Menu = menu,
                IsVisible = true,
            };
            _trayIcon.Clicked += (_, _) => RestoreAndActivate();

            // Keep tooltip + header on the single CurrentProfile/Title source.
            ViewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName is nameof(MainWindowViewModel.Title) or nameof(MainWindowViewModel.CurrentProfile))
                {
                    _trayIcon.ToolTipText = ViewModel.Title;
                    header.Header = ViewModel.CurrentProfile?.ProfileName ?? "Server";
                }
            };

            var icons = TrayIcon.GetIcons(Application.Current) ?? new TrayIcons();
            if (TrayIcon.GetIcons(Application.Current) is null)
                TrayIcon.SetIcons(Application.Current, icons);
            icons.Add(_trayIcon);

            Closed += (_, _) =>
            {
                icons.Remove(_trayIcon);
                _trayIcon.Dispose();
            };
        }
        catch (Exception ex)
        {
            // Windowed fallback: a DE without a system tray must never make the app unreachable.
            App.Instance.Services.GetService<Serilog.ILogger>()?.Warning(ex, "Tray icon unavailable; using windowed fallback");
            _trayIcon = null;
        }
    }

    private void RestoreAndActivate()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }
}
