using System;
using System.Threading.Tasks;
using ValheimServerGUI.Tools.Data;

namespace ValheimServerGUI.Core.Tests.Fakes
{
    /// <summary>
    /// In-memory <see cref="IFileProvider"/>: keeps the last-saved object and serves it back, with no
    /// real filesystem. Ported from the v2.4 tests; matches the nullable IFileProvider contract.
    /// </summary>
    public class MockDataFileProvider : IFileProvider
    {
        private object? DataFile;

        public void SetData<TFile>(TFile data)
        {
            DataFile = data;
        }

        public event EventHandler<object>? DataLoaded;
        public event EventHandler<object>? DataSaved;

        public Task<TFile?> LoadAsync<TFile>(string filePath) where TFile : class
        {
            if (DataFile != null) DataLoaded?.Invoke(this, DataFile);
            return Task.FromResult(DataFile as TFile);
        }

        public Task SaveAsync<TFile>(string filePath, TFile data) where TFile : class
        {
            DataFile = data;
            DataSaved?.Invoke(this, data);
            return Task.CompletedTask;
        }
    }
}
