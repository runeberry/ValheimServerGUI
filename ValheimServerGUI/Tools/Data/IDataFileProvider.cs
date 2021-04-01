using System;
using System.Threading.Tasks;

namespace ValheimServerGUI.Tools.Data
{
    public interface IDataFileProvider
    {
        event EventHandler<object> DataLoaded;

        event EventHandler<object> DataSaved;

        Task<TFile> LoadAsync<TFile>() where TFile : class;

        Task SaveAsync<TFile>(TFile data) where TFile : class;
    }
}
