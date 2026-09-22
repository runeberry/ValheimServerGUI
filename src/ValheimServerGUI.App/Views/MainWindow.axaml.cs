using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using ValheimServerGUI.App.Controls;
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
        viewModel.UpdateResultPrompt = msg => MessageBox.ConfirmAsync(this, "Check for Updates", msg);
        viewModel.StopTimedOutWarning += () =>
            _ = ShowMessageAsync("Server force-stopped",
                "The server did not shut down in time and was force-stopped. Recent world changes may not have been saved.");
        viewModel.MenuActionRequested += a => _ = HandleMenuActionAsync(a);
        viewModel.RemoveProfileRequested += name => _ = HandleRemoveProfileAsync(name);
        viewModel.Players.ViewDetailsRequested += player => _ = ShowPlayerDetailsAsync(player);
        viewModel.Players.AddByIdPrompt = usePermitted => new AddByIdWindow(usePermitted).ShowDialog<AddByIdResult?>(this);
        viewModel.UnsavedChangesPrompt = () => DialogGuards.ConfirmSaveDiscardCancelAsync(this);
        viewModel.MessagePrompt = ShowMessageAsync;
        viewModel.ImportConfirmPrompt = body =>
            MessageBox.ConfirmAsync(this, MainWindowViewModel.ImportDialogTitle, body, "Continue", "Cancel");
        viewModel.ConflictPrompt = body => MessageBox.ChooseAsync<RoleConflictChoice>(
            this, MainWindowViewModel.RoleConflictTitle, body,
            new MessageBoxButton("Use server profile", RoleConflictChoice.UseServerProfile, isDefault: true),
            new MessageBoxButton("Use roles from file", RoleConflictChoice.UseRolesFromFile),
            new MessageBoxButton("Cancel", RoleConflictChoice.Cancel, isCancel: true));

        Opened += OnOpened;
        SetUpTrayIcon();
        WireProfileDropdown(viewModel);
    }

    private bool _syncingProfileDropdown;

    // The menu-bar dropdown mirrors CurrentProfile (single source of truth) two-way: the VM drives the shown
    // value (derived, never mirrored), while a user pick routes through the same guarded switch as File > Load.
    private void WireProfileDropdown(MainWindowViewModel viewModel)
    {
        SyncProfileDropdown();
        ProfileDropdown.ValueChanged += OnProfileDropdownChanged;
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainWindowViewModel.CurrentProfile))
                SyncProfileDropdown();
        };
    }

    private void SyncProfileDropdown()
    {
        if (ViewModel is null) return;
        _syncingProfileDropdown = true;               // suppress the re-entrant ValueChanged for our own write
        ProfileDropdown.Value = ViewModel.CurrentProfile?.ProfileName;
        _syncingProfileDropdown = false;
    }

    private async void OnProfileDropdownChanged(object? sender, object? value)
    {
        if (_syncingProfileDropdown || ViewModel is null) return;
        if (value is not string name) return;

        if (!await ViewModel.RequestSwitchProfileAsync(name))
            SyncProfileDropdown();                     // cancelled: revert to the unchanged current profile
    }

    private static T Svc<T>() where T : notnull => App.Instance.Services.GetRequiredService<T>();

    private async Task HandleMenuActionAsync(MenuAction action)
    {
        if (ViewModel is null) return;

        switch (action)
        {
            case MenuAction.NewProfile:
                await CreateProfileAsync();
                break;
            case MenuAction.SaveProfile:
                Svc<IServerPreferencesProvider>().SavePreferences(ViewModel.BuildPreferences());
                break;
            case MenuAction.SaveProfileAs:
                await CreateProfileAsync(fromForm: true);
                break;
            case MenuAction.Preferences:
                await new PreferencesWindow(new PreferencesViewModel(
                    Svc<IUserPreferencesProvider>(), Svc<IStartupManager>())).ShowDialog(this);
                break;
            case MenuAction.SetDirectories:
                await new DirectoriesWindow(new DirectoriesViewModel(
                    Svc<IUserPreferencesProvider>(), Svc<IValheimPathResolver>(), Svc<IShellLauncher>())).ShowDialog(this);
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

    private async Task CreateProfileAsync(bool fromForm = false)
    {
        if (ViewModel is null) return;
        // Same unsaved-changes guard as a profile switch — creating a profile switches away from the current one.
        if (!await ViewModel.ConfirmDiscardCurrentAsync()) return;

        var serverPrefs = Svc<IServerPreferencesProvider>();
        var prefill = fromForm ? $"Copy of {ViewModel.CurrentProfile?.ProfileName}" : null;
        var name = await new TextPromptWindow("Server Profile Name", "Enter a server profile name:",
            prefill, maxLength: 30,
            validator: n => string.IsNullOrWhiteSpace(n) || n.Length > 30 || serverPrefs.LoadPreferences(n) is not null
                ? "Profile name must be 1-30 characters, and must not match an existing profile name."
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
        var confirm = await MessageBox.ConfirmAsync(this, "Remove Profile",
            $"Remove server profile '{profileName}'?");
        if (!confirm) return;

        Svc<IServerPreferencesProvider>().RemovePreferences(profileName);
        Svc<IServerManager>().Remove(profileName); // stop-then-dispose the profile's server, if any
    }

    private async Task ShowWorldPreferencesAsync()
    {
        var worldName = ViewModel?.Form.SelectedWorldName;
        if (string.IsNullOrWhiteSpace(worldName))
        {
            var message = ViewModel?.Form.UseNewWorld == true
                ? "Please enter a new world name before changing modifier settings."
                : "Unable to change modifier settings. No world is selected.";
            await ShowMessageAsync("World Name Missing", message);
            return;
        }

        await new WorldPreferencesWindow(new WorldPreferencesViewModel(
                Svc<IWorldPreferencesProvider>(), worldName, Svc<IShellLauncher>()))
            .ShowDialog(this);
    }

    private async Task ShowPlayerDetailsAsync(ValheimServerGUI.Game.PlayerInfo player)
        => await new PlayerDetailsWindow(
                new PlayerDetailsViewModel(Svc<IPlayerDataRepository>(), player.Key, Svc<IRuneberryApiClient>()))
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
        if (await MessageBox.ConfirmAsync(this, "File Not Found", body))
            await HandleMenuActionAsync(MenuAction.SetDirectories);
    }

    // Tab-visible lazy refresh (§10.2): the Details/Players 1s timers run only while their tab is shown.
    private void OnTabChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ViewModel is null) return;
        ViewModel.Details.SetActive(DetailsTab.IsSelected);
        ViewModel.Players.SetActive(PlayersTab.IsSelected);
    }

    private Task<CloudImportChoice> ShowCloudImportAsync(string worldName)
    {
        var message =
            $"Host the cloud world '{worldName}'?\n\n" +
            "This world is saved to Steam Cloud and must be brought into the server's local save folder to be hosted.\n\n" +
            "Move: bring the world over and remove the Steam Cloud copy.\n" +
            "Copy: bring a copy over and leave the Steam Cloud copy in place.";
        return MessageBox.ChooseAsync<CloudImportChoice>(this, "Import cloud world", message,
            new MessageBoxButton("Move", CloudImportChoice.Move),
            new MessageBoxButton("Copy", CloudImportChoice.Copy, isDefault: true),
            new MessageBoxButton("Cancel", CloudImportChoice.Cancel, isCancel: true));
    }

    private Task ShowMessageAsync(string title, string message)
        => MessageBox.ShowAsync(this, title, message);

    /// <summary>The per-window view-model (each window owns its own).</summary>
    public MainWindowViewModel? ViewModel { get; }

    private void OnNewWindowRequested()
        => App.Instance.Services.GetRequiredService<ShellCoordinator>().OpenNewWindow();

    // A per-window close never stops the server — servers are shared app-wide and outlive their windows
    // (WindowManager disposes this window's view-model on Closed, which only unsubscribes it). The graceful
    // save-flush now happens once, at app shutdown (App.OnShutdownRequested), over every running server.

    // Tray header + tooltip wording (WinForms parity): compact "ValheimServerGUI" tooltip, "Profile: {name}"
    // header, distinct from the window title.
    private static string TrayHeader(MainWindowViewModel vm)
        => vm.CurrentProfile is { } p ? $"Profile: {p.ProfileName}" : "No Profile Selected";

    private static string TrayTooltip(MainWindowViewModel vm)
        => vm.CurrentProfile is { } p ? $"ValheimServerGUI - {p.ProfileName}" : "ValheimServerGUI";

    // Tray icon + native menu (§10.4). Both buttons and tray items bind the SAME VM commands, so their
    // enablement is derived, never duplicated. Only under a desktop lifetime (skipped headless / no tray).
    private void SetUpTrayIcon()
    {
        if (ViewModel is null) return;
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime) return;

        try
        {
            var menu = new NativeMenu();

            var header = new NativeMenuItem { Header = TrayHeader(ViewModel) };
            header.Click += (_, _) => RestoreAndActivate();
            menu.Add(header);
            menu.Add(new NativeMenuItemSeparator());
            // Restore the WinForms control icons (header + Close stay icon-less). NativeMenuItem.Icon is a
            // Bitmap, which AppIcons.Get already returns; native disabled rendering is the OS's, so no
            // grayscale hook here.
            menu.Add(new NativeMenuItem { Header = "Start Server", Command = ViewModel.StartCommand, Icon = AppIcons.Get("Run_16x") });
            menu.Add(new NativeMenuItem { Header = "Restart Server", Command = ViewModel.RestartCommand, Icon = AppIcons.Get("Restart_16x") });
            menu.Add(new NativeMenuItem { Header = "Stop Server", Command = ViewModel.StopCommand, Icon = AppIcons.Get("Stop_16x") });
            menu.Add(new NativeMenuItemSeparator());
            menu.Add(new NativeMenuItem { Header = "Close", Command = ViewModel.CloseCommand });

            _trayIcon = new TrayIcon
            {
                Icon = AppIcon.Load(),
                ToolTipText = TrayTooltip(ViewModel),
                Menu = menu,
                IsVisible = true,
            };
            _trayIcon.Clicked += (_, _) => RestoreAndActivate();

            // Keep tooltip + header on the single CurrentProfile source.
            ViewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName is nameof(MainWindowViewModel.Title) or nameof(MainWindowViewModel.CurrentProfile))
                {
                    _trayIcon.ToolTipText = TrayTooltip(ViewModel);
                    header.Header = TrayHeader(ViewModel);
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
