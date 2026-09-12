using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Http;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Tools
{
    // §16.2 external-IP fallback chain (E52): try endpoints in order, first non-blank wins, keep prior on total failure.
    public class IpAddressProviderTests
    {
        private sealed class StubIpProvider : IpAddressProvider
        {
            public Dictionary<string, string?> Responses { get; } = new();
            public HashSet<string> ThrowFor { get; } = new();

            public StubIpProvider() : base(new RestClientContext(new LoggerConfiguration().CreateLogger(), new HttpClientProvider()))
            {
            }

            protected override IReadOnlyList<string> ExternalIpEndpoints { get; } = new[] { "one", "two", "three" };

            protected override Task<string?> FetchExternalIpAsync(string url)
            {
                if (ThrowFor.Contains(url)) throw new InvalidOperationException("boom");
                return Task.FromResult(Responses.TryGetValue(url, out var v) ? v : null);
            }
        }

        [Fact]
        public async Task First_non_blank_endpoint_wins()
        {
            var p = new StubIpProvider();
            p.Responses["one"] = null;
            p.Responses["two"] = "1.2.3.4";
            p.Responses["three"] = "5.6.7.8";

            await p.LoadExternalIpAddressAsync();

            Assert.Equal("1.2.3.4", p.ExternalIpAddress);
        }

        [Fact]
        public async Task A_throwing_endpoint_is_skipped()
        {
            var p = new StubIpProvider();
            p.ThrowFor.Add("one");
            p.Responses["two"] = "9.9.9.9";

            await p.LoadExternalIpAddressAsync();

            Assert.Equal("9.9.9.9", p.ExternalIpAddress);
        }

        [Fact]
        public async Task Result_is_trimmed()
        {
            var p = new StubIpProvider();
            p.Responses["one"] = "  4.4.4.4\n";

            await p.LoadExternalIpAddressAsync();

            Assert.Equal("4.4.4.4", p.ExternalIpAddress);
        }

        [Fact]
        public async Task Total_failure_keeps_the_previous_value()
        {
            var p = new StubIpProvider();
            p.Responses["one"] = "8.8.8.8";
            await p.LoadExternalIpAddressAsync();
            Assert.Equal("8.8.8.8", p.ExternalIpAddress);

            // Now every endpoint fails/blank.
            p.Responses["one"] = null;
            p.ThrowFor.Add("two");
            await p.LoadExternalIpAddressAsync();

            Assert.Equal("8.8.8.8", p.ExternalIpAddress); // unchanged (E52)
        }
    }
}
