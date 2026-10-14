using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ValheimServerGUI.Tools.Http;

namespace ValheimServerGUI.Core.Tests.Fakes
{
    /// <summary>
    /// Answers every request with <see cref="Respond"/> instead of the network, recording each request and which
    /// kind of client sent it, so a test can assert the exact route, headers and address-family choice.
    /// </summary>
    public class StubHttpClientProvider : IHttpClientProvider
    {
        public Func<HttpRequestMessage, HttpResponseMessage> Respond { get; set; } =
            _ => new HttpResponseMessage(HttpStatusCode.OK);

        public List<(HttpRequestMessage Request, bool IPv4Only)> Requests { get; } = new();

        public HttpClient CreateClient() => new(new Handler(this, ipv4Only: false));

        public HttpClient CreateIPv4Client() => new(new Handler(this, ipv4Only: true));

        /// <summary>Responds with <paramref name="status"/> and a JSON body.</summary>
        public void RespondWith(HttpStatusCode status, string json)
            => Respond = _ => new HttpResponseMessage(status) { Content = new StringContent(json) };

        private sealed class Handler : HttpMessageHandler
        {
            private readonly StubHttpClientProvider _owner;
            private readonly bool _ipv4Only;

            public Handler(StubHttpClientProvider owner, bool ipv4Only)
            {
                _owner = owner;
                _ipv4Only = ipv4Only;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                _owner.Requests.Add((request, _ipv4Only));
                return Task.FromResult(_owner.Respond(request));
            }
        }
    }
}
