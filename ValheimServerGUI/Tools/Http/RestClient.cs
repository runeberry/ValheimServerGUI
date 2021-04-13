using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace ValheimServerGUI.Tools.Http
{
    public class RestClient
    {
        public IRestClientContext Context { get; }

        public ILogger Logger => this.Context.Logger;

        public RestClient(IRestClientContext context)
        {
            this.Context = context;
        }

        public RestClientRequest Request(HttpMethod method, string uri, object payload = null)
        {
            return BuildRequest(method, uri, payload);
        }

        public RestClientRequest Get(string uri)
        {
            return BuildRequest(HttpMethod.Get, uri);
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
