using System;
using System.Threading.Tasks;
using ValheimServerGUI.Tools.Data;

namespace ValheimServerGUI.Tests.Tools
{
    public class MockDataFileProvider : IFileProvider
    {
        private object DataFile;

        public void SetData<TFile>(TFile data)
        {
            DataFile = data;
        }

        #region IDataFileProvider implementation

        public event EventHandler<object> DataLoaded;
        public event EventHandler<object> DataSaved;

        public Task<TFile> LoadAsync<TFile>(string filePath) where TFile : class
        {
            DataLoaded?.Invoke(this, DataFile);
            return Task.FromResult(DataFile as TFile);
        }

        public Task SaveAsync<TFile>(string filePath, TFile data) where TFile : class
        {
            DataFile = data;
            DataSaved?.Invoke(this, data);
            return Task.CompletedTask;
        }

        #endregion
    }
}
