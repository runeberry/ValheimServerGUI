using Newtonsoft.Json;
using System;

namespace ValheimServerGUI.Tools.Http
{
    public static class RestClientRequestExtensions
    {
        public static RestClientRequest WithResponseType<T>(this RestClientRequest request)
        {
            request.ResponseContentType = typeof(T);
            return request;
        }

        public static RestClientRequest WithCallback(this RestClientRequest request, HttpResponseHandler callback)
        {
            request.Callbacks.Add(callback);
            return request;
        }

        public static RestClientRequest WithCallback<T>(this RestClientRequest request, HttpResponseHandler<T> callback)
        {
            request.WithResponseType<T>();
            request.WithCallback((_, message) =>
            {
                if (callback == null) return;

                var responseContent = message.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                var typed = JsonConvert.DeserializeObject<T>(responseContent);

                callback.Invoke(request, typed);
            });

            return request;
        }
    }
}
