using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using ValheimServerGUI.App.Views;
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
            // The app lives until the last window closes (multi-window: §2.2), not tied to a single main window.
            desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;

            var window = Services.GetRequiredService<MainWindow>();
            window.Show();
        }

        base.OnFrameworkInitializationCompleted();
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
