using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using ValheimServerGUI.Properties;

namespace ValheimServerGUI.Serverless.Middleware
{
    [ApiController]
    public class RuneberryAuthMiddleware
    {
        private static readonly bool ApiKeyEnabled;

        static RuneberryAuthMiddleware()
        {
            ApiKeyEnabled = !string.IsNullOrWhiteSpace(ServerSecrets.RuneberryApiKeyHeader) && ServerSecrets.RuneberryApiKeyHeader.Any();
        }

        public static async Task Authorize(HttpContext context, Func<Task> next)
        {
            if (ApiKeyEnabled)
            {
                if (!context.Request.Headers.TryGetValue(ServerSecrets.RuneberryApiKeyHeader, out var apiKey))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new { message = "Missing API key" });
                    return;
                }

                if (!ServerSecrets.RuneberryServerApiKeys.Contains(apiKey))
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
