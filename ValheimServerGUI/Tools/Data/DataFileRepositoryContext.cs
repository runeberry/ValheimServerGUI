using Microsoft.Extensions.Logging;

namespace ValheimServerGUI.Tools.Data
{
    public interface IDataFileRepositoryContext
    {
        IDataFileProvider DataFileProvider { get; }

        ILogger Logger { get; }
    }

    public class DataFileRepositoryContext : IDataFileRepositoryContext
    {
        public IDataFileProvider DataFileProvider { get; }

        public ILogger Logger { get; }

        public DataFileRepositoryContext(IDataFileProvider dataFileProvider, ILogger logger)
        {
            this.DataFileProvider = dataFileProvider;
            this.Logger = logger;
        }
    }
}
