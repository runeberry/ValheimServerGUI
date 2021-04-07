using Newtonsoft.Json;
using System.Net.Http;
using ValheimServerGUI.Properties;
using ValheimServerGUI.Tools.Http;

namespace ValheimServerGUI.Tools
{
    public interface IExternalIpClient
    {
        event HttpResponseHandler<ExternalIpResponse> AddressReceived;

        void GetExternalIpAddress();
    }

    public class ExternalIpClient : RestClient, IExternalIpClient
    {
        public ExternalIpClient(IRestClientContext context) : base(context)
        {
        }

        public event HttpResponseHandler<ExternalIpResponse> AddressReceived;

        public void GetExternalIpAddress()
        {
            this.Request(HttpMethod.Get, Resources.UrlExternalIpLookup)
                .WithCallback(AddressReceived)
                .Send();
        }
    }

    public class ExternalIpResponse
    {
        [JsonProperty("ip")]
        public string Ip { get; set; }
    }
}
