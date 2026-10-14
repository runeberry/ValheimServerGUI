using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ValheimServerGUI.Tools.Http
{
    public interface IHttpClientProvider
    {
        public HttpClient CreateClient();

        /// <summary>A client that connects over IPv4 only, for requests whose answer depends on the address family
        /// they arrive on (the external-IP check). Every other request stays dual-stack.</summary>
        public HttpClient CreateIPv4Client();
    }

    public class HttpClientProvider : IHttpClientProvider
    {
        public HttpClient CreateClient()
        {
            return new HttpClient();
        }

        public HttpClient CreateIPv4Client()
        {
            var handler = new SocketsHttpHandler
            {
                ConnectCallback = (context, token) => ConnectIPv4Async(context.DnsEndPoint, Dns.GetHostAddressesAsync, token),
            };
            return new HttpClient(handler);
        }

        /// <summary>Resolves <paramref name="endPoint"/>'s host and connects to its IPv4 addresses only.</summary>
        internal static async ValueTask<Stream> ConnectIPv4Async(
            DnsEndPoint endPoint,
            Func<string, CancellationToken, Task<IPAddress[]>> resolveHost,
            CancellationToken cancellationToken)
        {
            var addresses = (await resolveHost(endPoint.Host, cancellationToken))
                .Where(a => a.AddressFamily == AddressFamily.InterNetwork)
                .ToArray();
            if (addresses.Length == 0)
                throw new HttpRequestException($"No IPv4 address found for {endPoint.Host}");

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(addresses, endPoint.Port, cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }
    }
}
