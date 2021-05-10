using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ValheimServerGUI.Properties;

namespace ValheimServerGUI.Serverless.Middleware
{
    [ApiController]
    public class RuneberryAuthMiddleware
    {
        private static readonly bool ApiKeyEnabled;

        static RuneberryAuthMiddleware()
        {
            ApiKeyEnabled = !string.IsNullOrWhiteSpace(Secrets.RuneberryApiKeyHeader) && Secrets.RuneberryApiKeyHeader.Any();
        }

        public static async Task Authorize(HttpContext context, Func<Task> next)
        {
            if (ApiKeyEnabled)
            {
                if (!context.Request.Headers.TryGetValue(Secrets.RuneberryApiKeyHeader, out var apiKey))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new { message = "Missing API key" });
                    return;
                }

                if (!Secrets.RuneberryServerApiKeys.Contains(apiKey))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new { message = "Invalid API key" });
                    return;
                }
            }

            await next?.Invoke();
        }
    }
}
