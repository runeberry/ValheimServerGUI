using Microsoft.Extensions.Logging;

namespace ValheimServerGUI.Tools.Data
{
    public interface IDataFileRepositoryContext
    {
        IFileProvider DataFileProvider { get; }

        ILogger Logger { get; }
    }

    public class DataFileRepositoryContext : IDataFileRepositoryContext
    {
        public IFileProvider DataFileProvider { get; }

        public ILogger Logger { get; }

        public DataFileRepositoryContext(IFileProvider dataFileProvider, ILogger logger)
        {
            DataFileProvider = dataFileProvider;
            Logger = logger;
        }
    }
}
