using System;
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
            _ = StartShellAsync();
        }

        base.OnFrameworkInitializationCompleted();
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
