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
        public string ExternalIpAddress { get; }

        public string InternalIpAddress { get; }

        event EventHandler<string> ExternalIpChanged;

        event EventHandler<string> InternalIpChanged;

        Task LoadExternalIpAddressAsync();

        Task LoadInternalIpAddressAsync();

        bool IsLocalUdpPortAvailable(params int[] ports);
    }

    public class IpAddressProvider : RestClient, IIpAddressProvider
    {
        public IpAddressProvider(IRestClientContext context) : base(context)
        {
        }

        #region IIpAddressProvider implementation

        private string _externalIpAddress;
        public string ExternalIpAddress
        {
            get => _externalIpAddress;
            private set
            {
                if (_externalIpAddress == value) return;
                _externalIpAddress = value;
                ExternalIpChanged?.Invoke(this, value);
            }
        }

        private string _internalIpAddress;
        public string InternalIpAddress
        {
            get => _internalIpAddress;
            private set
            {
                if (_internalIpAddress == value) return;
                _internalIpAddress = value;
                InternalIpChanged?.Invoke(this, value);
            }
        }

        public event EventHandler<string> ExternalIpChanged;

        public event EventHandler<string> InternalIpChanged;

        public Task LoadExternalIpAddressAsync()
        {
            return Get(Resources.UrlExternalIpLookup)
                .WithCallback<ExternalIpResponse>(OnExternalIpResponse)
                .SendAsync();
        }

        // Adapted from: https://stackoverflow.com/a/40528818/7071436
        public Task LoadInternalIpAddressAsync()
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
                Logger.Warning("Failed to find internal IP address: No network interfaces are UP with any IPv4 addresses");
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

            InternalIpAddress = results.FirstOrDefault();
            return Task.CompletedTask;
        }

        public bool IsLocalUdpPortAvailable(params int[] ports)
        {
            return !IPGlobalProperties
                .GetIPGlobalProperties()
                .GetActiveUdpListeners()
                .Any(p => ports.Contains(p.Port));
        }

        #endregion

        #region Non-public methods

        private void OnExternalIpResponse(object sender, ExternalIpResponse response)
        {
            if (string.IsNullOrWhiteSpace(response?.Ip)) return;
            ExternalIpAddress = response.Ip;
        }

        private class ExternalIpResponse
        {
            [JsonProperty("ip")]
            public string Ip { get; set; }
        }

        #endregion
    }
}
