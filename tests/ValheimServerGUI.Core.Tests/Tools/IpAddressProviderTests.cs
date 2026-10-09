using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Serilog;
using ValheimServerGUI.Core.Tests.Fakes;
using ValheimServerGUI.Properties;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Http;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Tools
{
    // §16.2 external IP: one IPv4-only request to the app's API (/ip-check); a failed or non-IPv4 answer keeps the
    // previous value (E52).
    public class IpAddressProviderTests
    {
        private readonly StubHttpClientProvider _http = new();

        private IpAddressProvider NewProvider()
            => new(new RestClientContext(new LoggerConfiguration().CreateLogger(), _http));

        private void AnswerIp(string ip) => _http.RespondWith(HttpStatusCode.OK, $"{{\"ip\":\"{ip}\"}}");

        // Deterministic because the socket stays bound for the whole assertion (the start flow's tests use a fake).
        [Fact]
        public void A_bound_udp_port_is_not_available()
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            var port = ((IPEndPoint)socket.LocalEndPoint!).Port;

            Assert.False(NewProvider().IsLocalUdpPortAvailable(port));
            Assert.False(NewProvider().IsLocalUdpPortAvailable(port - 1, port));
        }

        [Fact]
        public async Task Asks_the_api_ip_check_route_over_IPv4_with_the_client_key()
        {
            AnswerIp("203.0.113.7");
            var p = NewProvider();

            await p.LoadExternalIpAddressAsync();

            Assert.Equal("203.0.113.7", p.ExternalIpAddress);
            var (request, ipv4Only) = Assert.Single(_http.Requests);
            Assert.Equal($"{CoreConstants.UrlRuneberryApi}/ip-check", request.RequestUri!.ToString());
            Assert.Equal(new[] { ClientSecrets.RuneberryClientApiKey }, request.Headers.GetValues(ClientSecrets.RuneberryApiKeyHeader));
            Assert.True(ipv4Only);
        }

        [Fact]
        public async Task Result_is_trimmed()
        {
            _http.RespondWith(HttpStatusCode.OK, "{\"ip\":\"  4.4.4.4\\n\"}");
            var p = NewProvider();

            await p.LoadExternalIpAddressAsync();

            Assert.Equal("4.4.4.4", p.ExternalIpAddress);
        }

        [Theory]
        [InlineData("2001:db8::1")]
        [InlineData("not an ip")]
        [InlineData("1.2.3")]
        public async Task An_answer_that_is_not_IPv4_keeps_the_previous_value(string answer)
        {
            var p = NewProvider();
            AnswerIp("8.8.8.8");
            await p.LoadExternalIpAddressAsync();

            AnswerIp(answer);
            await p.LoadExternalIpAddressAsync();

            Assert.Equal("8.8.8.8", p.ExternalIpAddress);
        }

        [Theory]
        [InlineData(HttpStatusCode.InternalServerError, "{\"message\":\"no ip\"}")]
        [InlineData(HttpStatusCode.OK, "<!DOCTYPE html><html><head><title>Just a moment...</title></head></html>")]
        public async Task A_failed_request_keeps_the_previous_value(HttpStatusCode status, string body)
        {
            var p = NewProvider();
            AnswerIp("8.8.8.8");
            await p.LoadExternalIpAddressAsync();

            _http.RespondWith(status, body);
            await p.LoadExternalIpAddressAsync();

            Assert.Equal("8.8.8.8", p.ExternalIpAddress);
        }

        [Fact]
        public async Task An_unreachable_api_keeps_the_previous_value()
        {
            var p = NewProvider();
            AnswerIp("8.8.8.8");
            await p.LoadExternalIpAddressAsync();

            _http.Respond = _ => throw new HttpRequestException("no route");
            await p.LoadExternalIpAddressAsync();

            Assert.Equal("8.8.8.8", p.ExternalIpAddress);
        }
    }
}
