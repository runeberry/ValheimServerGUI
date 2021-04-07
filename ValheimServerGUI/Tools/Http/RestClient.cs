using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

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
