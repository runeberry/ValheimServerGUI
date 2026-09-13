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
using ValheimServerGUI.App.ViewModels.Dialogs;
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
        Icon = AppIcon.Load();
        ViewModel = viewModel;
        DataContext = viewModel;

        viewModel.NewWindowRequested += OnNewWindowRequested;
        viewModel.CloseRequested += Close;
        viewModel.CloudImportPrompt = ShowCloudImportAsync;
        viewModel.ErrorReported = msg => _ = ShowMessageAsync("Error starting server", msg);
        viewModel.UpdateResultPrompt = msg => new ConfirmWindow("Check for Updates", msg).ShowDialog<bool>(this);
        viewModel.StopTimedOutWarning += () =>
            _ = ShowMessageAsync("Server force-stopped",
                "The server did not shut down in time and was force-stopped. Recent world changes may not have been saved.");
        viewModel.MenuActionRequested += a => _ = HandleMenuActionAsync(a);
        viewModel.RemoveProfileRequested += name => _ = HandleRemoveProfileAsync(name);
        viewModel.Players.ViewDetailsRequested += player => _ = ShowPlayerDetailsAsync(player);

        Closing += OnClosing;
        Opened += OnOpened;
        SetUpTrayIcon();
    }

    private static T Svc<T>() where T : notnull => App.Instance.Services.GetRequiredService<T>();

    private async Task HandleMenuActionAsync(MenuAction action)
    {
        if (ViewModel is null) return;

        switch (action)
        {
            case MenuAction.NewProfile:
                await CreateProfileAsync("New Profile");
                break;
            case MenuAction.SaveProfile:
                Svc<IServerPreferencesProvider>().SavePreferences(ViewModel.BuildPreferences());
                break;
            case MenuAction.SaveProfileAs:
                await CreateProfileAsync("Save Profile As", fromForm: true);
                break;
            case MenuAction.Preferences:
                await new PreferencesWindow(new PreferencesViewModel(
                    Svc<IUserPreferencesProvider>(), Svc<IStartupManager>())).ShowDialog(this);
                break;
            case MenuAction.SetDirectories:
                await new DirectoriesWindow(new DirectoriesViewModel(
                    Svc<IUserPreferencesProvider>(), Svc<IValheimPathResolver>())).ShowDialog(this);
                break;
            case MenuAction.BugReport:
                await new BugReportWindow(new BugReportViewModel(Svc<IRuneberryApiClient>())).ShowDialog(this);
                break;
            case MenuAction.About:
                await new AboutWindow(new AboutViewModel(Svc<IShellLauncher>())).ShowDialog(this);
                break;
            case MenuAction.WorldPreferences:
                await ShowWorldPreferencesAsync();
                break;
        }
    }

    private async Task CreateProfileAsync(string title, bool fromForm = false)
    {
        if (ViewModel is null) return;

        var serverPrefs = Svc<IServerPreferencesProvider>();
        var name = await new TextPromptWindow(title, "Profile name:", maxLength: 64,
            validator: n => string.IsNullOrWhiteSpace(n) ? "Enter a profile name."
                : serverPrefs.LoadPreferences(n) is not null ? "A profile with that name already exists."
                : null).ShowDialog<string?>(this);

        if (string.IsNullOrWhiteSpace(name)) return;

        var prefs = fromForm
            ? ViewModel.Form.ToPreferences(new ServerPreferences { ProfileName = name })
            : new ServerPreferences { ProfileName = name };
        serverPrefs.SavePreferences(prefs);
        ViewModel.LoadProfile(prefs);
    }

    private async Task HandleRemoveProfileAsync(string profileName)
    {
        var confirm = await new ConfirmWindow("Remove Profile",
            $"Remove the profile '{profileName}'? This cannot be undone.").ShowDialog<bool>(this);
        if (confirm) Svc<IServerPreferencesProvider>().RemovePreferences(profileName);
    }

    private async Task ShowWorldPreferencesAsync()
    {
        var worldName = ViewModel?.Form.SelectedWorldName;
        if (string.IsNullOrWhiteSpace(worldName))
        {
            await ShowMessageAsync("World Preferences", "Select or name a world first.");
            return;
        }

        await new WorldPreferencesWindow(new WorldPreferencesViewModel(
                Svc<IWorldPreferencesProvider>(), worldName, Svc<IShellLauncher>()))
            .ShowDialog(this);
    }

    private async Task ShowPlayerDetailsAsync(ValheimServerGUI.Game.PlayerInfo player)
        => await new PlayerDetailsWindow(new PlayerDetailsViewModel(Svc<IPlayerDataRepository>(), player.Key))
            .ShowDialog(this);

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (ViewModel is { AutoStartOnLoad: true })
        {
            await ViewModel.StartServerAsync(isManual: false);
            return;
        }

        await CheckServerExePathAsync();
    }

    // Startup parity: warn if the configured server exe is missing and offer to open the Directories dialog.
    // Only under a real desktop lifetime — never prompt in a headless/test run.
    private async Task CheckServerExePathAsync()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime) return;
        if (ViewModel?.GetMissingServerExeError() is not { } error) return;

        var body = $"{error}\n\n" +
            "This may occur if you do not have Valheim Dedicated Server installed, or if you have installed " +
            "it in a different directory. See Help for more info.\n\n" +
            "Would you like to change your directories now?";
        if (await new ConfirmWindow("File Not Found", body).ShowDialog<bool>(this))
            await HandleMenuActionAsync(MenuAction.SetDirectories);
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
                Icon = AppIcon.Load(),
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
