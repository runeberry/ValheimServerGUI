using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Windows.Forms;
using ValheimServerGUI.Tests.Tools;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Data;
using ValheimServerGUI.Tools.Processes;

namespace ValheimServerGUI.Tests
{
    public static class TestServices
    {
        public static IServiceProvider Build()
        {
            var services = new ServiceCollection();
            Program.ConfigureServices(services);

            services.Replace(ServiceDescriptor.Singleton<IFileProvider, MockDataFileProvider>());
            services.Replace(ServiceDescriptor.Singleton<IProcessProvider, MockProcessProvider>());

            return services.BuildServiceProvider();
        }

        public static TForm BuildForm<TForm>() where TForm : Form
        {
            var services = Build();

            var formProvider = services.GetRequiredService<IFormProvider>();
            return formProvider.GetForm<TForm>();
        }
    }
}
