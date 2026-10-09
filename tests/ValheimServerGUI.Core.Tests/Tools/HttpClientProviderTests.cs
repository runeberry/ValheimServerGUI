using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ValheimServerGUI.Tools.Http;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Tools
{
    // The IPv4-only client (used for the external-IP check against a dual-stack host) connects to a host's IPv4
    // addresses and never to its IPv6 ones.
    public class HttpClientProviderTests
    {
        private static Func<string, CancellationToken, Task<IPAddress[]>> Resolves(params IPAddress[] addresses)
            => (_, _) => Task.FromResult(addresses);

        [Fact]
        public async Task Connects_to_the_hosts_IPv4_address()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var accept = listener.AcceptSocketAsync(TestContext.Current.CancellationToken);

            await using var stream = await HttpClientProvider.ConnectIPv4Async(
                new DnsEndPoint("api.example", port), Resolves(IPAddress.IPv6Loopback, IPAddress.Loopback), TestContext.Current.CancellationToken);

            using var accepted = await accept;
            Assert.Equal(AddressFamily.InterNetwork, ((IPEndPoint)accepted.RemoteEndPoint!).AddressFamily);
        }

        [Fact]
        public async Task A_host_with_only_IPv6_addresses_is_not_connected_to()
        {
            var e = await Assert.ThrowsAsync<HttpRequestException>(async () => await HttpClientProvider.ConnectIPv4Async(
                new DnsEndPoint("api.example", 443), Resolves(IPAddress.IPv6Loopback, IPAddress.Parse("2001:db8::1")), CancellationToken.None));
            Assert.Contains("api.example", e.Message);
        }

        // End to end through a real HttpClient, so the handler wiring (not just the connect step) is covered.
        [Fact]
        public async Task The_IPv4_client_completes_a_request()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var token = TestContext.Current.CancellationToken;
            var serve = Task.Run(async () =>
            {
                using var client = await listener.AcceptTcpClientAsync(token);
                using var reader = new StreamReader(client.GetStream(), leaveOpen: true);
                while (!string.IsNullOrEmpty(await reader.ReadLineAsync(token))) { } // drain the request headers
                await client.GetStream().WriteAsync("HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nok"u8.ToArray(), token);
            }, token);

            using var http = new HttpClientProvider().CreateIPv4Client();
            var body = await http.GetStringAsync($"http://localhost:{port}/", token);

            Assert.Equal("ok", body);
            await serve;
        }
    }
}
