using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Windows.Forms;
using ValheimServerGUI.Forms;
using ValheimServerGUI.Game;
using ValheimServerGUI.Properties;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Data;
using ValheimServerGUI.Tools.Http;
using ValheimServerGUI.Tools.Logging;
using ValheimServerGUI.Tools.Preferences;
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
            if (!VersionCheck()) return;

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            var services = new ServiceCollection();
            ConfigureServices(services);
            using var serviceProvider = services.BuildServiceProvider();
            ExceptionHandler = serviceProvider.GetRequiredService<IExceptionHandler>();

            try
            {
                Application.Run(serviceProvider.GetRequiredService<MainWindow>());
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

            services.AddSingleton<IUserPreferences>((_) =>
            {
                var userPreferences = new UserPreferences();
                userPreferences.LoadFile();
                return userPreferences;
            });
            services.AddSingleton<IDataFileRepositoryContext, DataFileRepositoryContext>();
            services.AddSingleton<IDataFileProvider, JsonDataFileProvider>();
            services.AddSingleton<IFormProvider, FormProvider>();
            services.AddSingleton<IProcessProvider, ProcessProvider>();
            services.AddSingleton<ILogger>(applicationLogger);
            services.AddSingleton<IEventLogger>(applicationLogger);
            services.AddSingleton<IHttpClientProvider, HttpClientProvider>();
            services.AddSingleton<IRestClientContext, RestClientContext>();
            services.AddSingleton<IIpAddressProvider, IpAddressProvider>();
            services.AddSingleton<IGitHubClient, GitHubClient>();
            services.AddSingleton<IExceptionHandler, ExceptionHandler>();

            // Game & server data
            services
                .AddSingleton<IValheimFileProvider, ValheimFileProvider>()
                .AddSingleton<IPlayerDataRepository, PlayerDataRepository>()
                .AddSingleton<ValheimServerLogger>()
                .AddSingleton<ValheimServer>();

            // Forms
            services
                .AddSingleton<MainWindow>()
                .AddSingleton<DirectoriesForm>()
                .AddSingleton<AboutForm>()
                .AddTransient<PlayerDetailsForm>();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ExceptionHandler.HandleException(e.ExceptionObject as Exception, "Unhandled Exception");
        }

        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            ExceptionHandler.HandleException(e.Exception, "Thread Exception");
        }

        private static bool VersionCheck()
        {
            var dotnetVersion = AssemblyHelper.GetDotnetRuntimeVersion();

            if (dotnetVersion.Major < 5)
            {
                var nl = Environment.NewLine;
                var result = MessageBox.Show(
                    $"ValheimServerGUI requires the .NET 5.0 Desktop Runtime (or higher) to be installed.{nl}" +
                    $"You are currently using .NET {dotnetVersion}.{nl}{nl}" +
                    "Would you like to go to the download page now?",
                    ".NET Upgrade Required",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    WebHelper.OpenWebAddress(Resources.UrlDotnetDownload);
                }

                return false;
            }

            return true;
        }
    }
}
