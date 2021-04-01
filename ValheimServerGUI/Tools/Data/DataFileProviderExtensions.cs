using System.Threading.Tasks;

namespace ValheimServerGUI.Tools.Data
{
    public static class DataFileProviderExtensions
    {
        public static void Load<TFile>(this IDataFileProvider provider)
            where TFile : class
        {
            Task.Run(() => provider.LoadAsync<TFile>());
        }

        public static void Save<TFile>(this IDataFileProvider provider, TFile data)
            where TFile : class
        {
            Task.Run(() => provider.SaveAsync(data));
        }
    }
}
