using System;
using System.Linq;
using System.Reflection;
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
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;

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

    // §2.4 "safe shutdowns", now aggregated over every running server. On the first request, decide via
    // CloseDecider; if servers are running, cancel, stop them all with the graceful save-flush (blocking off
    // the UI thread until each reports Stopped), then re-trigger — the second pass is pre-approved.
    private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        if (_shutdownApproved) return;
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;

        var manager = Services.GetRequiredService<IServerManager>();
        var prompt = Services.GetRequiredService<IUserPrompt>();
        var statuses = manager.All.Select(s => s.Status).ToList();

        switch (CloseDecider.Decide(statuses, IsOsShutdown(e), prompt, "Warning"))
        {
            case CloseDecision.Proceed:
                manager.StopAllAndDispose(); // all Stopped already → returns immediately
                _shutdownApproved = true;
                return;

            case CloseDecision.Cancel:
                e.Cancel = true;
                return;

            case CloseDecision.StopThenClose:
                e.Cancel = true;
                _ = StopAllThenShutdownAsync(desktop, manager);
                return;
        }
    }

    // ShutdownRequestedEventArgs.IsOSShutdown is internal in Avalonia 12.1, so read it reflectively: on an OS
    // shutdown / logoff we flush saves silently rather than popping a modal nobody can answer. Any failure
    // falls back to false → the normal prompt path, which is the safe default.
    private static bool IsOsShutdown(ShutdownRequestedEventArgs e)
    {
        try
        {
            var prop = e.GetType().GetProperty(
                "IsOSShutdown", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return prop?.GetValue(e) is true;
        }
        catch
        {
            return false;
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
