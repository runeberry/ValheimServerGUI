using System.Threading.Tasks;

namespace ValheimServerGUI.Tools.Data
{
    public static class DataFileProviderExtensions
    {
        public static void Load<TFile>(this IFileProvider provider, string filePath)
            where TFile : class
        {
            Task.Run(() => provider.LoadAsync<TFile>(filePath));
        }

        public static void Save<TFile>(this IFileProvider provider, string filePath, TFile data)
            where TFile : class
        {
            Task.Run(() => provider.SaveAsync(filePath, data));
        }
    }
}
