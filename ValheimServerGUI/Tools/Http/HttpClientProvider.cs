using System.Net.Http;

namespace ValheimServerGUI.Tools.Http
{
    public interface IHttpClientProvider
    {
        public HttpClient CreateClient();
    }

    public class HttpClientProvider : IHttpClientProvider
    {
        public HttpClient CreateClient()
        {
            return new HttpClient();
        }
    }
}
