using System;
using System.Threading.Tasks;

namespace ValheimServerGUI.Tools.Data
{
    public interface IFileProvider
    {
        event EventHandler<object> DataLoaded;

        event EventHandler<object> DataSaved;

        Task<TFile> LoadAsync<TFile>(string filePath) where TFile : class;

        Task SaveAsync<TFile>(string filePath, TFile data) where TFile : class;
    }
}
