using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using ValheimServerGUI.Tools.Http;

namespace ValheimServerGUI.Tools
{
    public interface IIpAddressProvider
    {
        public string? ExternalIpAddress { get; }

        public string? InternalIpAddress { get; }

        event EventHandler<string?> ExternalIpChanged;

        event EventHandler<string?> InternalIpChanged;

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

        private string? _externalIpAddress;
        public string? ExternalIpAddress
        {
            get => _externalIpAddress;
            private set
            {
                if (_externalIpAddress == value) return;
                _externalIpAddress = value;
                ExternalIpChanged?.Invoke(this, value);
            }
        }

        private string? _internalIpAddress;
        public string? InternalIpAddress
        {
            get => _internalIpAddress;
            private set
            {
                if (_internalIpAddress == value) return;
                _internalIpAddress = value;
                InternalIpChanged?.Invoke(this, value);
            }
        }

        public event EventHandler<string?>? ExternalIpChanged;

        public event EventHandler<string?>? InternalIpChanged;

        /// <summary>
        /// External-IP endpoints tried in order (§16.2 fallback chain, E52): ipify → ifconfig.co →
        /// icanhazip. The first non-blank result wins; if all fail the previous value is kept.
        /// </summary>
        protected virtual IReadOnlyList<string> ExternalIpEndpoints { get; } = new[]
        {
            CoreConstants.UrlExternalIpLookup,
            CoreConstants.UrlExternalIpLookupFallback1,
            CoreConstants.UrlExternalIpLookupFallback2,
        };

        public async Task LoadExternalIpAddressAsync()
        {
            foreach (var url in ExternalIpEndpoints)
            {
                try
                {
                    var ip = await FetchExternalIpAsync(url);
                    if (!string.IsNullOrWhiteSpace(ip))
                    {
                        ExternalIpAddress = ip.Trim();
                        return;
                    }
                }
                catch (Exception e)
                {
                    Logger.Warning(e, "External IP lookup failed for {Url}", url);
                }
            }

            // E52: every endpoint failed/blank — keep the previous value (no-op).
        }

        /// <summary>Fetches the external IP from one endpoint. ipify returns <c>{"ip":…}</c>; the others return the bare IP.</summary>
        protected virtual async Task<string?> FetchExternalIpAsync(string url)
        {
            using var client = Context.HttpClientProvider.CreateClient();
            var body = (await client.GetStringAsync(url)).Trim();
            if (body.StartsWith('{'))
                return JsonConvert.DeserializeObject<ExternalIpResponse>(body)?.Ip;
            return body;
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

            // Prefer addresses from the DHCP server if they're available. PrefixOrigin is a
            // Windows-only API; the in-lambda OS guard both satisfies the platform analyzer and
            // implements E51 (on Linux the guard is false, so we fall back to all UP IPv4 addresses).
            var dhcpAddresses = addresses.Where(ip => OperatingSystem.IsWindows() && ip.PrefixOrigin == PrefixOrigin.Dhcp);
            var eligibleAddresses = dhcpAddresses.Any() ? dhcpAddresses : addresses;

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

        private class ExternalIpResponse
        {
            [JsonProperty("ip")]
            public string? Ip { get; set; }
        }

        #endregion
    }
}
