using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        public static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var services = new ServiceCollection();
            ConfigureServices(services);
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

        public static void ConfigureServices(IServiceCollection services)
        {
            // Tools
            var applicationLogger = new ApplicationLogger();

            services
                .AddSingleton<IDataFileRepositoryContext, DataFileRepositoryContext>()
                .AddSingleton<IFileProvider, JsonFileProvider>()
                .AddSingleton<IFormProvider, FormProvider>()
                .AddSingleton<IProcessProvider, ProcessProvider>()
                .AddSingleton<ILogger>(applicationLogger)
                .AddSingleton<IEventLogger>(applicationLogger)
                .AddSingleton<IHttpClientProvider, HttpClientProvider>()
                .AddSingleton<IRestClientContext, RestClientContext>()
                .AddSingleton<IIpAddressProvider, IpAddressProvider>()
                .AddSingleton<IGitHubClient, GitHubClient>()
                .AddSingleton<ISoftwareUpdateProvider, SoftwareUpdateProvider>()
                .AddSingleton<IExceptionHandler, ExceptionHandler>()
                .AddSingleton<IRuneberryApiClient, RuneberryApiClient>();

            // Game & server data
            services
                .AddSingleton<IValheimFileProvider, ValheimFileProvider>()
                .AddSingleton<IPlayerDataRepository, PlayerDataRepository>()
                .AddSingleton<IUserPreferencesProvider, UserPreferencesProvider>()
                .AddSingleton<ValheimServerLogger>()
                .AddSingleton<ValheimServer>();

            // Forms
            services
                .AddSingleton<SplashForm>()
                .AddSingleton<MainWindow>()
                .AddSingleton<DirectoriesForm>()
                .AddSingleton<PreferencesForm>()
                .AddSingleton<BugReportForm>()
                .AddSingleton<AboutForm>()
                .AddTransient<PlayerDetailsForm>();
        }
    }
}
