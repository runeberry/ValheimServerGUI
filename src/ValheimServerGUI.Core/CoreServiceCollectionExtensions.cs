using System;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Data;
using ValheimServerGUI.Tools.Http;
using ValheimServerGUI.Tools.Logging;
using ValheimServerGUI.Tools.Processes;

namespace ValheimServerGUI
{
    public static class CoreServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the headless service layer (the Tools + Game groups of the old Program.ConfigureServices),
        /// selecting the per-OS bucket-A implementations. The Phase 2 shell supplies the remaining
        /// composition: the bucket-B seams (<see cref="IUserPrompt"/>, <see cref="IStartupManager"/>,
        /// <see cref="IShellLauncher"/>), <see cref="IStartupArgsProvider"/> (it needs the process args),
        /// and the views.
        /// </summary>
        public static IServiceCollection AddValheimCore(this IServiceCollection services)
        {
            // Bucket A: OS-dispatched path/steam resolvers.
            if (OperatingSystem.IsWindows())
            {
                services
                    .AddSingleton<IValheimPathResolver, WindowsValheimPathResolver>()
                    .AddSingleton<ISteamPathResolver, WindowsSteamPathResolver>();
            }
            else
            {
                services
                    .AddSingleton<IValheimPathResolver, LinuxValheimPathResolver>()
                    .AddSingleton<ISteamPathResolver, LinuxSteamPathResolver>();
            }

            // Tools
            services
                .AddSingleton<IDataFileRepositoryContext, DataFileRepositoryContext>()
                .AddSingleton<IFileProvider, JsonFileProvider>()
                .AddSingleton<IProcessProvider, ProcessProvider>()
                .AddSingleton<ApplicationLogger>()
                .AddSingleton<Serilog.ILogger>(sp => sp.GetRequiredService<ApplicationLogger>())
                .AddSingleton<IApplicationLogger>(sp => sp.GetRequiredService<ApplicationLogger>())
                .AddSingleton<IHttpClientProvider, HttpClientProvider>()
                .AddSingleton<IRestClientContext, RestClientContext>()
                .AddSingleton<IIpAddressProvider, IpAddressProvider>()
                .AddSingleton<IGitHubClient, GitHubClient>()
                .AddSingleton<ISoftwareUpdateProvider, SoftwareUpdateProvider>()
                .AddSingleton<IExceptionHandler, ExceptionHandler>()
                .AddSingleton<IRuneberryApiClient, RuneberryApiClient>();

            // Game & server data
            services
                .AddSingleton<IPlayerDataRepository, PlayerDataRepository>()
                .AddSingleton<IUserPreferencesProvider, UserPreferencesProvider>()
                .AddSingleton<IServerPreferencesProvider, ServerPreferencesProvider>()
                .AddSingleton<IWorldPreferencesProvider, WorldPreferencesProvider>()
                .AddSingleton<ISteamCloudWorldProvider, SteamCloudWorldProvider>()
                .AddTransient<ValheimServer>()
                // The manager is the single construction site for servers (one per profile, shared app-wide);
                // the factory lets it build a fresh transient ValheimServer on first ask for a profile.
                .AddSingleton<Func<ValheimServer>>(sp => sp.GetRequiredService<ValheimServer>)
                .AddSingleton<IServerManager, ServerManager>();

            return services;
        }
    }
}
