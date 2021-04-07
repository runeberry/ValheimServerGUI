using System.Net.Http;

namespace ValheimServerGUI.Tools.Http
{
    public class RestClient
    {
        public IRestClientContext Context { get; }

        public RestClient(IRestClientContext context)
        {
            this.Context = context;
        }

        public RestClientRequest Request(HttpMethod method, string uri, object payload = null)
        {
            return BuildRequest(method, uri, payload);
        }

        private RestClientRequest BuildRequest(HttpMethod method, string uri, object payload = null)
        {
            return new RestClientRequest(this)
            {
                Method = method,
                Uri = uri,
                RequestContent = payload,
            };
        }
    }
}
