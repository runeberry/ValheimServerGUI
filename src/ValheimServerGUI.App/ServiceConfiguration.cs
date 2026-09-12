using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using ValheimServerGUI.App.Infrastructure;
using ValheimServerGUI.App.Services;
using ValheimServerGUI.App.Startup;
using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.App;

/// <summary>
/// The desktop shell's composition. Starts from the Core service layer (<see cref="CoreServiceCollectionExtensions.AddValheimCore"/>)
/// and adds the bucket-B seams the Core leaves to the shell (<see cref="IUserPrompt"/>, <see cref="IStartupManager"/>,
/// <see cref="IShellLauncher"/>), the <see cref="IStartupArgsProvider"/> instance (it needs the process args), and the
/// windows + view-models.
/// </summary>
internal static class ServiceConfiguration
{
    public static IServiceProvider BuildServiceProvider(string[] args, SingleInstanceManager? singleInstance = null)
        => ConfigureServices(new ServiceCollection(), args, singleInstance).BuildServiceProvider();

    public static IServiceCollection ConfigureServices(
        IServiceCollection services, string[] args, SingleInstanceManager? singleInstance = null)
    {
        services.AddValheimCore();

        // Startup args (bucket B: needs the process args, so it is registered here rather than in Core).
        services.AddSingleton<IStartupArgsProvider>(new StartupArgsProvider(args));

        // Single-instance guard (created in Program.Main so the mutex is acquired before the app builds).
        services.AddSingleton(singleInstance ?? new SingleInstanceManager());

        // Bucket-B seams (the OS-integration the Core leaves to the shell).
        services.AddSingleton<ISystemShell, SystemShell>();
        services.AddSingleton<IUserPrompt, DialogUserPrompt>();
        services.AddSingleton<IShellLauncher, ShellLauncher>();
        services.AddSingleton<IStartupStrategy>(_ => CreateStartupStrategy());
        services.AddSingleton<IStartupManager, StartupManager>();

        // Shell lifetime / startup.
        services.AddSingleton<WindowManager>();
        services.AddSingleton<StartupService>();
        services.AddSingleton<ShellCoordinator>();

        // View-models. MainWindowViewModel is per-window (transient, owns a transient ValheimServer); the
        // factory hands "New Window" / each auto-start profile its own instance over the shared singletons.
        services.AddTransient<MainWindowViewModel>();
        services.AddSingleton<Func<MainWindowViewModel>>(sp => sp.GetRequiredService<MainWindowViewModel>);
        services.AddTransient<SplashViewModel>();

        return services;
    }

    // Picks the per-OS run-on-login mechanism, stamped with this process's executable path so the
    // registration relaunches the actual running app.
    private static IStartupStrategy CreateStartupStrategy()
    {
        var execPath = Environment.ProcessPath ?? string.Empty;

        if (OperatingSystem.IsWindows())
            return new WindowsRunKeyStrategy(AppConstants.StartupKey, execPath);

        var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (string.IsNullOrEmpty(configHome))
            configHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        var autostartDir = Path.Combine(configHome, "autostart");

        return new LinuxAutostartStrategy(autostartDir, execPath, AppConstants.ProductName, AppConstants.StartupKey);
    }
}
