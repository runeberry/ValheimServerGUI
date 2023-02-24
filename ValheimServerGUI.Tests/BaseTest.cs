using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Windows.Forms;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tests.Tools;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Data;
using ValheimServerGUI.Tools.Http;
using ValheimServerGUI.Tools.Processes;

namespace ValheimServerGUI.Tests
{
    public partial class BaseTest
    {
        protected IServiceCollection ServiceCollection { get; }

        protected IServiceProvider ServiceProvider { get; }

        protected MockDataFileProvider MockDataFileProvider { get; }

        protected MockHttpClientProvider MockHttpClientProvider { get; }

        protected MockProcessProvider MockProcessProvider { get; }

        protected MockUserPreferencesProvider MockUserPreferencesProvider { get; }

        public BaseTest()
        {
            ServiceCollection = new ServiceCollection();
            Program.ConfigureServices(ServiceCollection, Array.Empty<string>());

            MockDataFileProvider = new();
            MockProcessProvider = new();
            MockHttpClientProvider = new();
            MockUserPreferencesProvider = new();

            ServiceCollection.Replace(ServiceDescriptor.Singleton<IFileProvider>(MockDataFileProvider));
            ServiceCollection.Replace(ServiceDescriptor.Singleton<IProcessProvider>(MockProcessProvider));
            ServiceCollection.Replace(ServiceDescriptor.Singleton<IHttpClientProvider>(MockHttpClientProvider));
            ServiceCollection.Replace(ServiceDescriptor.Singleton<IUserPreferencesProvider>(MockUserPreferencesProvider));

            ServiceProvider = ServiceCollection.BuildServiceProvider();
        }

        protected TService GetService<TService>()
        {
            return ServiceProvider.GetRequiredService<TService>();
        }

        protected TForm GetForm<TForm>() where TForm : Form
        {
            var formProvider = ServiceProvider.GetRequiredService<IFormProvider>();
            return formProvider.GetForm<TForm>();
        }
    }
}
