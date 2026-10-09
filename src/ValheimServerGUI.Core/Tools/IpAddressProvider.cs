using Newtonsoft.Json;
using System;
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
        /// Asks the app's API for the address this machine's requests arrive from (§16.2). The request goes over
        /// IPv4 because the API host is dual-stack and players join on the IPv4 address. Anything other than an IPv4
        /// address, or a failed request, keeps the previous value (E52).
        /// </summary>
        public async Task LoadExternalIpAddressAsync()
        {
            try
            {
                var ip = (await FetchExternalIpAsync())?.Trim();
                if (IsIPv4(ip))
                {
                    ExternalIpAddress = ip;
                    return;
                }

                if (!string.IsNullOrEmpty(ip))
                    Logger.Warning("External IP lookup did not return an IPv4 address");
            }
            catch (Exception e)
            {
                Logger.Warning(e, "External IP lookup failed");
            }
        }

        // TryParse alone is lenient ("1.2.3" parses as 1.2.0.3), so also require the text to be the address's
        // canonical dotted-quad form.
        private static bool IsIPv4(string? text) =>
            IPAddress.TryParse(text, out var address)
            && address.AddressFamily == AddressFamily.InterNetwork
            && address.ToString() == text;

        /// <summary>Fetches the external IP from the API's <c>/ip-check</c> route (<c>{"ip":…}</c>).</summary>
        /// <remarks>Routed through <see cref="RestClient"/> (not a raw HttpClient) so the call is logged at the
        /// shared HTTP chokepoint — Info on success, Error on failure — like every other external request.</remarks>
        private async Task<string?> FetchExternalIpAsync()
        {
            var response = await Get($"{CoreConstants.UrlRuneberryApi}/ip-check")
                .WithRuneberryApiKey()
                .WithIPv4Only()
                .SendAsync<ExternalIpResponse>();
            return response?.Ip;
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
