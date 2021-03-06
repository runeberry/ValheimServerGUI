﻿using Newtonsoft.Json;
using System;
using System.Net.Http;

namespace ValheimServerGUI.Tools.Http
{
    public static class RestClientRequestExtensions
    {
        public static RestClientRequest WithClientOptions(this RestClientRequest request, Action<HttpClient> options)
        {
            request.ClientBuilders.Add(options);
            return request;
        }

        public static RestClientRequest WithRequestOptions(this RestClientRequest request, Action<HttpRequestMessage> options)
        {
            request.RequestBuilders.Add(options);
            return request;
        }

        public static RestClientRequest WithResponseType<T>(this RestClientRequest request)
        {
            request.ResponseContentType = typeof(T);
            return request;
        }

        public static RestClientRequest WithHeader(this RestClientRequest request, string key, string value)
        {
            request.RequestBuilders.Add(message =>
            {
                message.Headers.TryAddWithoutValidation(key, value);
            });

            return request;
        }

        public static RestClientRequest WithCallback(this RestClientRequest request, EventHandler<HttpResponseMessage> callback)
        {
            request.Callbacks.Add(callback);
            return request;
        }

        public static RestClientRequest WithCallback<TResponse>(this RestClientRequest request, EventHandler<TResponse> callback)
        {
            request.WithResponseType<TResponse>();
            request.WithCallback((_, message) =>
            {
                if (callback == null) return;

                var responseContent = message.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                var typed = JsonConvert.DeserializeObject<TResponse>(responseContent);

                callback.Invoke(request, typed);
            });

            return request;
        }
    }
}
