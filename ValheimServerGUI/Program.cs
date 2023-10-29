using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Windows.Forms;
using ValheimServerGUI.Forms;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Data;
using ValheimServerGUI.Tools.Http;
using ValheimServerGUI.Tools.Logging;
using ValheimServerGUI.Tools.Processes;

namespace ValheimServerGUI
{
    public static class Program
    {
        private static IExceptionHandler ExceptionHandler;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var services = new ServiceCollection();
            ConfigureServices(services, args);
            using var serviceProvider = services.BuildServiceProvider();
            ExceptionHandler = serviceProvider.GetRequiredService<IExceptionHandler>();

            try
            {
                Application.Run(serviceProvider.GetRequiredService<SplashForm>());
            }
            catch (Exception e)
            {
                ExceptionHandler.HandleException(e, "Application Run Exception");
            }
        }

        public static void ConfigureServices(IServiceCollection services, string[] args)
        {
            var startupArgsProvider = new StartupArgsProvider(args);

            // Tools
            services
                .AddSingleton<IDataFileRepositoryContext, DataFileRepositoryContext>()
                .AddSingleton<IFileProvider, JsonFileProvider>()
                .AddSingleton<IFormProvider, FormProvider>()
                .AddSingleton<IProcessProvider, ProcessProvider>()
                .AddSingleton<ApplicationLogger>()
                .AddSingleton<ILogger>(sp => sp.GetRequiredService<ApplicationLogger>())
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
                .AddSingleton<IStartupArgsProvider>(startupArgsProvider)
                .AddTransient<ValheimServer>();

            // Forms
            services
                .AddSingleton<SplashForm>()
                .AddTransient<MainWindow>()
                .AddSingleton<DirectoriesForm>()
                .AddSingleton<PreferencesForm>()
                .AddSingleton<BugReportForm>()
                .AddSingleton<AboutForm>()
                .AddTransient<PlayerDetailsForm>()
                .AddTransient<WorldPreferencesForm>();
        }
    }
}
