using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Forms;
using ValheimServerGUI.Forms;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Preferences;
using ValheimServerGUI.Tools.Processes;

namespace ValheimServerGUI
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var services = new ServiceCollection();
            ConfigureServices(services);
            using var serviceProvider = services.BuildServiceProvider();

            Application.Run(serviceProvider.GetRequiredService<MainWindow>());
        }

        private static void ConfigureServices(IServiceCollection services)
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

            // Game & server data
            services.AddSingleton<ValheimServer>();

            // Forms
            services
                .AddSingleton<MainWindow>()
                .AddSingleton<DirectoriesForm>()
                .AddSingleton<AboutForm>();
        }
    }
}
