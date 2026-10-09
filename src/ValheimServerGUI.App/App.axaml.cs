using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using ValheimServerGUI.App.Infrastructure;
using ValheimServerGUI.App.Startup;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.App.Views;
using ValheimServerGUI.App.Views.Dialogs;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Logging;

namespace ValheimServerGUI.App;

public partial class App : Application
{
    private IServiceProvider? _services;

    /// <summary>Used by the Avalonia visual designer / test harness, which construct the App without a provider.</summary>
    public App()
    {
    }

    /// <summary>Composition-root constructor: the running app is handed the fully-built provider from <c>Program.Main</c>.</summary>
    public App(IServiceProvider services)
    {
        _services = services;
    }

    /// <summary>The running <see cref="App"/> instance (the desktop lifetime constructs it).</summary>
    internal static App Instance => (App)Current!;

    /// <summary>The DI provider. Lazily builds a default one for the designer / any host that did not inject one.</summary>
    public IServiceProvider Services => _services ??= ServiceConfiguration.BuildServiceProvider(Array.Empty<string>());

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        RegisterGlobalExceptionHandlers();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // The app lives until the last window closes (multi-window: §2.2). The splash is shown first,
            // then closed once the main window(s) are up, so the count never hits zero mid-startup.
            desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;
            // Servers are shared app-wide and outlive individual windows, so the save-flush guard lives here
            // (app shutdown) rather than per-window close.
            desktop.ShutdownRequested += OnShutdownRequested;
            _ = StartShellAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private bool _shutdownApproved;
    private bool _exitPromptOpen;

    // §2.4 "safe shutdowns", aggregated over every running server. Two entry points:
    //
    // 1. OnMainWindowClosing — the user closes the last main window (title-bar X, tray Close). The question is asked
    //    HERE, while the window still exists: the close is cancelled, the prompt is owned by (and centered on) that
    //    window, and Yes stops every server before exiting while No simply leaves the window open.
    // 2. OnShutdownRequested — Avalonia's app-level shutdown. On a window close it only fires AFTER the last window has
    //    been destroyed, so it must never prompt: with no window left, a prompt has no owner, and closing that prompt
    //    is itself "the last window closed", which re-raises this event (an endless prompt loop). It only flushes
    //    saves: an OS shutdown / logoff, or any exit that bypassed the window prompt, stops servers gracefully first.
    private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        if (_shutdownApproved) return;
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;

        var manager = Services.GetRequiredService<IServerManager>();
        if (manager.All.All(s => s.Status == ServerStatus.Stopped))
        {
            Services.GetRequiredService<IApplicationLogger>().Information("Shutting down application");
            manager.StopAllAndDispose(); // all Stopped already → returns immediately
            _shutdownApproved = true;
            return;
        }

        e.Cancel = true;
        _ = StopAllThenShutdownAsync(desktop, manager);
    }

    /// <summary>
    /// Called from a main window's <see cref="Window.Closing"/>. When it is the last main window and a server is still
    /// running or stopping, cancels the close and asks what to do (see <see cref="OnShutdownRequested"/>).
    /// </summary>
    internal void OnMainWindowClosing(Window window, WindowClosingEventArgs e)
    {
        if (_shutdownApproved) return;
        if (e.CloseReason is WindowCloseReason.ApplicationShutdown or WindowCloseReason.OSShutdown) return;
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;

        // Another main window stays open: closing this one doesn't exit the app, and servers outlive windows.
        if (desktop.Windows.OfType<MainWindow>().Any(w => w != window)) return;

        var manager = Services.GetRequiredService<IServerManager>();
        var statuses = manager.All.Select(s => s.Status).ToList();
        if (statuses.All(s => s == ServerStatus.Stopped)) return; // nothing to flush: close normally

        e.Cancel = true;
        if (_exitPromptOpen) return; // a second close click while the prompt is already up
        _ = ConfirmExitAsync(window, desktop, manager, statuses);
    }

    private async Task ConfirmExitAsync(
        Window window, IClassicDesktopStyleApplicationLifetime desktop, IServerManager manager, IReadOnlyCollection<ServerStatus> statuses)
    {
        _exitPromptOpen = true;
        CloseDecision decision;
        try
        {
            decision = await CloseDecider.DecideAsync(
                statuses, message => MessageBox.ConfirmAsync(window, "Warning", message));
        }
        finally
        {
            _exitPromptOpen = false;
        }

        switch (decision)
        {
            case CloseDecision.Cancel:
                return; // the close was cancelled; the window stays open

            case CloseDecision.Proceed: // "exit anyway" while a server is still shutting down
                Services.GetRequiredService<IApplicationLogger>().Information("Shutting down application");
                _shutdownApproved = true;
                desktop.Shutdown();
                return;

            case CloseDecision.StopThenClose:
                await StopAllThenShutdownAsync(desktop, manager);
                return;
        }
    }

    private async Task StopAllThenShutdownAsync(IClassicDesktopStyleApplicationLifetime desktop, IServerManager manager)
    {
        // Off the UI thread: block until every server reports Stopped (the graceful world-save flush
        // completes — do not return early or §16.2/E9 regresses), then approve and re-trigger the shutdown.
        await Task.Run(manager.StopAllAndDispose);
        _shutdownApproved = true;
        Dispatcher.UIThread.Post(() => desktop.Shutdown());
    }

    // Splash → async startup tasks → main window(s) → hide splash → begin serving second-launch forwards.
    private async Task StartShellAsync()
    {
        try
        {
            ApplyTheme(Services.GetRequiredService<IUserPreferencesProvider>().LoadPreferences().Theme);

            var splashViewModel = Services.GetRequiredService<SplashViewModel>();
            var splash = new SplashWindow(splashViewModel);
            splash.Show();

            var coordinator = Services.GetRequiredService<ShellCoordinator>();
            await coordinator.RunStartupTasksAsync(splashViewModel);
            coordinator.CreateAndShowStartupWindows();

            splash.Close();

            Services.GetRequiredService<IApplicationLogger>().Information(
                "ValheimServerGUI v{version} - Loaded OK", AssemblyHelper.GetApplicationVersion());

            var singleInstance = Services.GetRequiredService<SingleInstanceManager>();
            singleInstance.ArgsReceived += forwarded =>
                Dispatcher.UIThread.Post(() => coordinator.OpenOrFocusProfile(forwarded.FirstOrDefault()));
            singleInstance.StartListening();
        }
        catch (Exception ex)
        {
            HandleException(ex, "Startup failed");
        }
    }

    // Route the three unhandled-exception channels (§9.6) through the Core exception handler.
    private void RegisterGlobalExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                HandleException(ex, "Unhandled AppDomain exception");
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            HandleException(e.Exception, "Unobserved task exception");
            e.SetObserved();
        };

        Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            HandleException(e.Exception, "Unhandled dispatcher exception");
            e.Handled = true;
        };
    }

    /// <summary>Applies the user's theme preference (§16.2). Called at startup and when Preferences saves.</summary>
    public void ApplyTheme(AppTheme theme) => RequestedThemeVariant = theme switch
    {
        AppTheme.Light => ThemeVariant.Light,
        AppTheme.Dark => ThemeVariant.Dark,
        _ => ThemeVariant.Default,
    };

    private void HandleException(Exception ex, string context)
    {
        try
        {
            Services.GetRequiredService<IExceptionHandler>().HandleException(ex, context);
        }
        catch
        {
            // Never let the exception handler itself take the process down.
        }
    }
}
