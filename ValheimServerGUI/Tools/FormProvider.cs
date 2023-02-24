using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Forms;

namespace ValheimServerGUI.Tools
{
    public class FormProvider : IFormProvider
    {
        private readonly IServiceProvider ServiceProvider;

        public FormProvider(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public T GetForm<T>() where T : Form
        {
            return ServiceProvider.GetRequiredService<T>();
        }
    }

    public interface IFormProvider
    {
        T GetForm<T>() where T : Form;
    }
}
