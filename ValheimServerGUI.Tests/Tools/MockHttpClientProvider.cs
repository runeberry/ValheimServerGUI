using Moq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using ValheimServerGUI.Tools.Http;

namespace ValheimServerGUI.Tests.Tools
{
    public class MockHttpClientProvider : IHttpClientProvider
    {
        private Action<HttpResponseMessage> BuildDefaultResponse = res => res.StatusCode = HttpStatusCode.NotImplemented;
        
        private readonly List<(Func<HttpRequestMessage, bool>, Action<HttpResponseMessage>)> Setups = new();

        public MockHttpClientProvider SetResponse(Func<HttpRequestMessage, bool> condition, Action<HttpResponseMessage> responseBuilder)
        {
            this.Setups.Add((condition, responseBuilder));
            return this;
        }

        public MockHttpClientProvider SetDefaultResponse(Action<HttpResponseMessage> responseBuilder)
        {
            this.BuildDefaultResponse = responseBuilder;
            return this;
        }

        public HttpClient CreateClient()
        {
            var mockClient = new Mock<HttpClient>();

            mockClient
                .Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>()))
                .ReturnsAsync(() => GetResponseMessage(this.BuildDefaultResponse));

            foreach (var (condition, responseBuilder) in this.Setups)
            {
                mockClient
                    .Setup(c => c.SendAsync(It.Is<HttpRequestMessage>(r => condition(r))))
                    .ReturnsAsync(() => GetResponseMessage(responseBuilder));
            }

            return mockClient.Object;
        }

        private static HttpResponseMessage GetResponseMessage(Action<HttpResponseMessage> builder)
        {
            var response = new HttpResponseMessage();
            builder(response);
            return response;
        }
    }
}
