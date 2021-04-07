using Microsoft.Extensions.Logging;
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

        protected IRestClientContext Context => this.Client.Context;

        public HttpMethod Method { get; set; }

        public string Uri { get; set; }

        public object RequestContent { get; set; }

        public object ResponseContent { get; set; }

        public Type ResponseContentType { get; set; }

        public List<EventHandler<HttpResponseMessage>> Callbacks { get; } = new();

        public RestClientRequest(RestClient client)
        {
            this.Client = client;
        }

        public void Send()
        {
            Task.Run(async () =>
            {
                var logAddress = $"{this.Method} {this.Uri}";

                try
                {
                    var client = this.Context.HttpClientProvider.CreateClient();
                    var requestMessage = new HttpRequestMessage(this.Method, this.Uri);

                    if (this.RequestContent != null)
                    {
                        var strPayload = JsonConvert.SerializeObject(this.RequestContent);
                        requestMessage.Content = new StringContent(strPayload, Encoding.UTF8, "application/json");
                    }

                    var responseMessage = await client.SendAsync(requestMessage);
                    var statusCode = (int)responseMessage.StatusCode;
                    
                    
                    if (!responseMessage.IsSuccessStatusCode)
                    {
                        this.Context.Logger.LogError("HTTP request was not successful ({0}): {1}", statusCode, logAddress);
                        return;
                    }

                    if (this.ResponseContentType != null)
                    {
                        var responseContentStr = await responseMessage.Content.ReadAsStringAsync();
                        this.ResponseContent = JsonConvert.DeserializeObject(responseContentStr, this.ResponseContentType);
                    }

                    this.Context.Logger.LogTrace("HTTP request was successful ({0}): {1}", statusCode, logAddress);

                    foreach (var callback in this.Callbacks)
                    {
                        try
                        {
                            callback?.Invoke(this, responseMessage);
                        }
                        catch (Exception callbackException)
                        {
                            // Log the error, but keep iterating over callbacks
                            this.Context.Logger.LogError(callbackException, "HTTP request callback encountered an unexpected error: {0}", logAddress);
                            this.Context.Logger.LogError(callbackException.Message);
                            this.Context.Logger.LogError(callbackException.StackTrace);
                        }
                    }
                }
                catch (Exception e)
                {
                    this.Context.Logger.LogError(e, "HTTP request encountered an unexpected error: {0}", logAddress);
                    this.Context.Logger.LogError(e.Message);
                    this.Context.Logger.LogError(e.StackTrace);
                    return;
                }
            });
        }
    }
}
