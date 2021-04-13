using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using ValheimServerGUI.Properties;
using ValheimServerGUI.Tools.Http;

namespace ValheimServerGUI.Tools
{
    public interface IIpAddressProvider
    {
        event EventHandler<string> ExternalIpReceived;

        event EventHandler<string> InternalIpReceived;

        Task GetExternalIpAddressAsync();

        Task GetInternalIpAddressAsync();
    }

    public class IpAddressProvider : RestClient, IIpAddressProvider
    {
        public IpAddressProvider(IRestClientContext context) : base(context)
        {
        }

        public event EventHandler<string> ExternalIpReceived;

        public event EventHandler<string> InternalIpReceived;

        public Task GetExternalIpAddressAsync()
        {
            return this.Get(Resources.UrlExternalIpLookup)
                .WithCallback<ExternalIpResponse>(this.OnExternalIpResponse)
                .SendAsync();
        }

        // Adapted from: https://stackoverflow.com/a/40528818/7071436
        public Task GetInternalIpAddressAsync()
        {
            // Extract the IPv4 addresses from all network interfaces that are currently "up"
            var addresses = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up)
                .Select(n => n.GetIPProperties())
                .Where(p => p.GatewayAddresses.Any())
                .SelectMany(p => p.UnicastAddresses)
                .Where(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip.Address));

            if (!addresses.Any())
            {
                this.Logger.LogWarning("Failed to find internal IP address: No network interfaces are UP with any IPv4 addresses");
                return Task.CompletedTask;
            }

            // Prefer addresses from the DHCP server if they're available
            var eligibleAddresses = addresses.Where(ip => ip.PrefixOrigin == PrefixOrigin.Dhcp);
            if (!eligibleAddresses.Any())
            {
                eligibleAddresses = addresses;
            }

            // If multiple IPs are found, return the first one in alphabetical order (just for consistency)
            var results = eligibleAddresses
                .Select(ip => ip.Address.ToString())
                .Where(str => !string.IsNullOrWhiteSpace(str))
                .OrderBy(str => str);

            var result = results.FirstOrDefault();

            if (result != null)
            {
                this.Logger.LogTrace("Found {0} internal IP address(es): {1}", results.Count(), string.Join(", ", results));
                this.InternalIpReceived?.Invoke(this, result);
            }

            return Task.CompletedTask;
        }

        private void OnExternalIpResponse(object sender, ExternalIpResponse response)
        {
            if (string.IsNullOrWhiteSpace(response?.Ip)) return;
            this.ExternalIpReceived?.Invoke(this, response.Ip);
        }

        private class ExternalIpResponse
        {
            [JsonProperty("ip")]
            public string Ip { get; set; }
        }
    }
}
