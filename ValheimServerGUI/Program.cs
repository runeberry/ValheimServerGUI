using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Windows.Forms;
using ValheimServerGUI.Forms;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Logging;
using ValheimServerGUI.Tools.Preferences;
using ValheimServerGUI.Tools.Processes;

namespace ValheimServerGUI
{
    public static class Program
    {
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

            Application.Run(serviceProvider.GetRequiredService<MainWindow>());
        }

        public static void ConfigureServices(IServiceCollection services)
        {
            // Tools
            services.AddSingleton<IUserPreferences>((_) =>
            {
                var userPreferences = new UserPreferences();
                userPreferences.LoadFile();
                return userPreferences;
            });
            services.AddSingleton<IFormProvider, FormProvider>();
            services.AddSingleton<IProcessProvider, ProcessProvider>();
            services.AddSingleton<ILogger, ApplicationLogger>();
            services.AddSingleton<IEventLogger, ApplicationLogger>();

            // Game & server data
            services
                .AddSingleton<IValheimFileProvider, ValheimFileProvider>()
                .AddSingleton<IPlayerDataFileProvider, PlayerDataFileProvider>()
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
    }
}
