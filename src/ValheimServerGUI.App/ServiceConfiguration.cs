using System;
using Microsoft.Extensions.DependencyInjection;
using ValheimServerGUI.App.Views;
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
    public static IServiceProvider BuildServiceProvider(string[] args)
        => ConfigureServices(new ServiceCollection(), args).BuildServiceProvider();

    public static IServiceCollection ConfigureServices(IServiceCollection services, string[] args)
    {
        services.AddValheimCore();

        // Startup args (bucket B: needs the process args, so it is registered here rather than in Core).
        services.AddSingleton<IStartupArgsProvider>(new StartupArgsProvider(args));

        // Bucket-B seams. Wave 0 registers a placeholder prompt so the exception handler is resolvable;
        // Wave 1 replaces these with the real Window-backed implementations.
        services.AddSingleton<IUserPrompt, NoOpUserPrompt>();

        // Windows / view-models.
        services.AddTransient<MainWindow>();

        return services;
    }
}
