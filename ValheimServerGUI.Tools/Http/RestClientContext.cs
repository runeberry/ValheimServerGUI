using Serilog;

namespace ValheimServerGUI.Tools.Http
{
    public interface IRestClientContext
    {
        ILogger Logger { get; }

        IHttpClientProvider HttpClientProvider { get; }
    }

    public class RestClientContext : IRestClientContext
    {
        public ILogger Logger { get; }

        public IHttpClientProvider HttpClientProvider { get; }

        public RestClientContext(ILogger logger, IHttpClientProvider provider)
        {
            Logger = logger;
            HttpClientProvider = provider;
        }
    }
}
