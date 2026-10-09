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
using ValheimServerGUI.Localization;
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
        // Closing the last window exits the app: App asks about running servers before this window goes away.
        Closing += (_, e) => App.Instance.OnMainWindowClosing(this, e);
        viewModel.CloudImportPrompt = ShowCloudImportAsync;
        viewModel.ErrorReported = msg => _ = ShowMessageAsync(Strings.Prompt_ErrorStartingServer_Title, msg);
        viewModel.UpdateResultPrompt = msg => MessageBox.ConfirmAsync(this, Strings.Prompt_CheckForUpdates_Title, msg);
        viewModel.StopTimedOutWarning += () =>
            _ = ShowMessageAsync(Strings.Prompt_ForceStopped_Title,
                Strings.Prompt_ForceStopped_Message);
        viewModel.MenuActionRequested += a => _ = HandleMenuActionAsync(a);
        viewModel.RemoveProfileRequested += name => _ = HandleRemoveProfileAsync(name);
        viewModel.Players.ViewDetailsRequested += player => _ = ShowPlayerDetailsAsync(player);
        viewModel.Players.ManagePlayersRequested += () => _ = ShowManagePlayersAsync();
        viewModel.Players.AddPlayerPrompt = options => new AddPlayerWindow(options).ShowDialog<AddPlayerResult?>(this);
        viewModel.UnsavedChangesPrompt = () => DialogGuards.ConfirmSaveDiscardCancelAsync(this);
        viewModel.MessagePrompt = ShowMessageAsync;
        viewModel.ImportConfirmPrompt = body =>
            MessageBox.ConfirmAsync(this, MainWindowViewModel.ImportDialogTitle, body, Strings.Common_Continue, Strings.Common_Cancel);
        viewModel.ConflictPrompt = body => MessageBox.ChooseAsync<RoleConflictChoice>(
            this, MainWindowViewModel.RoleConflictTitle, body,
            new MessageBoxButton(Strings.Prompt_RoleConflict_UseServerProfile, RoleConflictChoice.UseServerProfile, isDefault: true),
            new MessageBoxButton(Strings.Prompt_RoleConflict_UseRolesFromFile, RoleConflictChoice.UseRolesFromFile),
            new MessageBoxButton(Strings.Common_Cancel, RoleConflictChoice.Cancel, isCancel: true));

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
        var prefill = fromForm ? string.Format(Strings.Prompt_ProfileName_CopyOf, ViewModel.CurrentProfile?.ProfileName) : null;
        var name = await new TextPromptWindow(Strings.Prompt_ProfileName_Title, Strings.Prompt_ProfileName_Message,
            prefill, maxLength: 30,
            validator: n => string.IsNullOrWhiteSpace(n) || n.Length > 30 || serverPrefs.LoadPreferences(n) is not null
                ? Strings.Prompt_ProfileName_Invalid
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
        var confirm = await MessageBox.ConfirmAsync(this, Strings.Prompt_RemoveProfile_Title,
            string.Format(Strings.Prompt_RemoveProfile_Message, profileName));
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
                ? Strings.Prompt_WorldNameMissing_NewWorld
                : Strings.Prompt_WorldNameMissing_NoWorld;
            await ShowMessageAsync(Strings.Prompt_WorldNameMissing_Title, message);
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

    private async Task ShowManagePlayersAsync()
    {
        var repo = Svc<IPlayerDataRepository>();
        var api = Svc<IRuneberryApiClient>();
        await new ManagePlayersWindow(new ManagePlayersViewModel(Svc<IUserPreferencesProvider>(), repo, api), repo, api)
            .ShowDialog(this);
    }

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

        var body = string.Format(Strings.Prompt_ServerExeNotFound_Message, error);
        if (await MessageBox.ConfirmAsync(this, Strings.Prompt_ServerExeNotFound_Title, body))
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
        var message = string.Format(Strings.Prompt_CloudImport_Message, worldName);
        return MessageBox.ChooseAsync<CloudImportChoice>(this, Strings.Prompt_CloudImport_Title, message,
            new MessageBoxButton(Strings.Prompt_CloudImport_Move, CloudImportChoice.Move),
            new MessageBoxButton(Strings.Prompt_CloudImport_Copy, CloudImportChoice.Copy, isDefault: true),
            new MessageBoxButton(Strings.Common_Cancel, CloudImportChoice.Cancel, isCancel: true));
    }

    private Task ShowMessageAsync(string title, string message)
        => MessageBox.ShowAsync(this, title, message);

    /// <summary>The per-window view-model (each window owns its own).</summary>
    public MainWindowViewModel? ViewModel { get; }

    private void OnNewWindowRequested()
        => App.Instance.Services.GetRequiredService<ShellCoordinator>().OpenNewWindow();

    // Closing one of several windows never stops a server — servers are shared app-wide and outlive their windows
    // (WindowManager disposes this window's view-model on Closed, which only unsubscribes it). Closing the LAST
    // window exits the app, so App.OnMainWindowClosing asks about running servers first (see App.OnShutdownRequested).

    // Tray header + tooltip wording (WinForms parity): compact "ValheimServerGUI" tooltip, "Profile: {name}"
    // header, distinct from the window title.
    private static string TrayHeader(MainWindowViewModel vm)
        => vm.CurrentProfile is { } p ? string.Format(Strings.Tray_Profile, p.ProfileName) : Strings.Tray_NoProfile;

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
            menu.Add(new NativeMenuItem { Header = Strings.ServerAction_Start, Command = ViewModel.StartCommand, Icon = AppIcons.Get("Run_16x") });
            menu.Add(new NativeMenuItem { Header = Strings.ServerAction_Restart, Command = ViewModel.RestartCommand, Icon = AppIcons.Get("Restart_16x") });
            menu.Add(new NativeMenuItem { Header = Strings.ServerAction_Stop, Command = ViewModel.StopCommand, Icon = AppIcons.Get("Stop_16x") });
            menu.Add(new NativeMenuItemSeparator());
            menu.Add(new NativeMenuItem { Header = Strings.Common_Close, Command = ViewModel.CloseCommand });

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
