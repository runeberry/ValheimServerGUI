using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ValheimServerGUI.Tools
{
    public static class DiscordWebhookClient
    {
        private static readonly HttpClient HttpClient = CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15),
            };

            client.DefaultRequestHeaders.UserAgent.ParseAdd("ValheimServerGUI-DiscordStatus/2.0");
            return client;
        }

        public static async Task SendAsync(string webhookUrl, string message)
        {
            var payload = JsonSerializer.Serialize(new
            {
                content = message
            });

            await PostJsonAsync(webhookUrl, payload);
        }

        public static async Task SendEmbedAsync(
            string webhookUrl,
            string title,
            string description,
            int color,
            params (string Name, string Value, bool Inline)[] fields)
        {
            var embedFields = fields
                .Where(f => !string.IsNullOrWhiteSpace(f.Name))
                .Select(f => new
                {
                    name = f.Name,
                    value = string.IsNullOrWhiteSpace(f.Value) ? "N/A" : f.Value,
                    inline = f.Inline
                })
                .ToArray();

            var payload = JsonSerializer.Serialize(new
            {
                embeds = new[]
                {
                    new
                    {
                        title,
                        description,
                        color,
                        fields = embedFields,
                        timestamp = DateTimeOffset.UtcNow.ToString("O"),
                        footer = new
                        {
                            text = "ValheimServerGUI"
                        }
                    }
                }
            });

            await PostJsonAsync(webhookUrl, payload);
        }

        private static async Task PostJsonAsync(string webhookUrl, string payload)
        {
            if (string.IsNullOrWhiteSpace(webhookUrl))
            {
                throw new ArgumentException("Discord webhook URL is empty.", nameof(webhookUrl));
            }

            if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out var uri) ||
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Discord webhook URL must be a valid HTTPS URL.", nameof(webhookUrl));
            }

            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            using var response = await HttpClient.PostAsync(uri, content);

            if (!response.IsSuccessStatusCode)
            {
                var responseText = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Discord webhook returned {(int)response.StatusCode} {response.ReasonPhrase}: {responseText}");
            }
        }
    }
}
