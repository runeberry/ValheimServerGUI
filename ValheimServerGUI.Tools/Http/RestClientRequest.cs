using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ValheimServerGUI.Tools.Http
{
    public class RestClientRequest
    {
        protected RestClient Client { get; }

        protected IRestClientContext Context => Client.Context;

        public HttpMethod Method { get; set; }

        public string Uri { get; set; }

        public object RequestContent { get; set; }

        public object ResponseContent { get; set; }

        public Type ResponseContentType { get; set; }

        public List<Action<HttpClient>> ClientBuilders { get; } = new();

        public List<Action<HttpRequestMessage>> RequestBuilders { get; } = new();

        public List<EventHandler<HttpResponseMessage>> Callbacks { get; } = new();

        public RestClientRequest(RestClient client)
        {
            Client = client;
        }

        public async Task<TResponse> SendAsync<TResponse>()
            where TResponse : class
        {
            await this.WithResponseType<TResponse>().SendAsync();
            return ResponseContent as TResponse;
        }

        public async Task<HttpResponseMessage> SendAsync()
        {
            var logAddress = $"{Method} {Uri}";

            try
            {
                var client = Context.HttpClientProvider.CreateClient();

                foreach (var clientBuilder in ClientBuilders)
                {
                    clientBuilder(client);
                }

                var requestMessage = new HttpRequestMessage(Method, Uri);

                if (RequestContent != null)
                {
                    var strPayload = JsonConvert.SerializeObject(RequestContent);
                    requestMessage.Content = new StringContent(strPayload, Encoding.UTF8, "application/json");
                }

                foreach (var requestBuilder in RequestBuilders)
                {
                    requestBuilder(requestMessage);
                }

                var responseMessage = await client.SendAsync(requestMessage);
                var statusCode = (int)responseMessage.StatusCode;

                if (!responseMessage.IsSuccessStatusCode)
                {
                    Context.Logger.Error("HTTP request was not successful ({0}): {1}", statusCode, logAddress);
                    return responseMessage;
                }

                if (ResponseContentType != null)
                {
                    var responseContentStr = await responseMessage.Content.ReadAsStringAsync();
                    ResponseContent = JsonConvert.DeserializeObject(responseContentStr, ResponseContentType);
                }

                Context.Logger.Debug("HTTP request was successful ({0}): {1}", statusCode, logAddress);

                foreach (var callback in Callbacks)
                {
                    try
                    {
                        callback?.Invoke(this, responseMessage);
                    }
                    catch (Exception callbackException)
                    {
                        // Log the error, but keep iterating over callbacks
                        Context.Logger.Error(callbackException, "HTTP request callback encountered an unexpected error: {0}", logAddress);
                        Context.Logger.Error(callbackException.Message);
                        Context.Logger.Error(callbackException.StackTrace);
                    }
                }

                return responseMessage;
            }
            catch (Exception e)
            {
                Context.Logger.Error(e, "HTTP request encountered an unexpected error: {0}", logAddress);
                Context.Logger.Error(e.Message);
                Context.Logger.Error(e.StackTrace);
                return null;
            }
        }
    }
}
