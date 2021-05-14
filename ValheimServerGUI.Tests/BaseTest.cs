using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Windows.Forms;
using ValheimServerGUI.Tests.Tools;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Data;
using ValheimServerGUI.Tools.Http;
using ValheimServerGUI.Tools.Processes;

namespace ValheimServerGUI.Tests
{
    public class BaseTest
    {
        protected IServiceCollection ServiceCollection { get; }

        protected IServiceProvider ServiceProvider { get; }

        protected MockDataFileProvider MockDataFileProvider { get; }

        protected MockHttpClientProvider MockHttpClientProvider { get; }

        protected MockProcessProvider MockProcessProvider { get; }

        public BaseTest()
        {
            this.ServiceCollection = new ServiceCollection();
            Program.ConfigureServices(this.ServiceCollection);

            this.MockDataFileProvider = new();
            this.MockProcessProvider = new();
            this.MockHttpClientProvider = new();

            this.ServiceCollection.Replace(ServiceDescriptor.Singleton<IFileProvider>(this.MockDataFileProvider));
            this.ServiceCollection.Replace(ServiceDescriptor.Singleton<IProcessProvider>(this.MockProcessProvider));
            this.ServiceCollection.Replace(ServiceDescriptor.Singleton<IHttpClientProvider>(this.MockHttpClientProvider));

            this.ServiceProvider = this.ServiceCollection.BuildServiceProvider();
        }

        public TService GetService<TService>()
        {
            return this.ServiceProvider.GetRequiredService<TService>();
        }

        public TForm GetForm<TForm>() where TForm : Form
        {
            var formProvider = this.ServiceProvider.GetRequiredService<IFormProvider>();
            return formProvider.GetForm<TForm>();
        }
    }
}
