using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using ValheimServerGUI.Properties;
using ValheimServerGUI.Tools.Http;

namespace ValheimServerGUI.Tools
{
    public interface IRuneberryApiClient
    {
        Task SendCrashReportAsync(CrashReport report);
    }

    public class RuneberryApiClient : RestClient, IRuneberryApiClient
    {
        public RuneberryApiClient(IRestClientContext context) : base(context)
        {
        }

        public async Task SendCrashReportAsync(CrashReport report)
        {
            var response = await Post($"{Resources.UrlRuneberryApi}/crash-report", report)
                .WithHeader(Secrets.RuneberryApiKeyHeader, Secrets.RuneberryClientApiKey)
                .SendAsync();

            if (response == null || !response.IsSuccessStatusCode)
            {
                string message;

                try
                {
                    if (response != null)
                    {
                        var rawResponse = await response.Content.ReadAsStringAsync();
                        var exceptionResponse = JsonConvert.DeserializeObject<ExceptionResponse>(rawResponse);
                        message = $"({(int)response.StatusCode}) {exceptionResponse.Message}";
                    }
                    else
                    {
                        message = "Unable to reach Runeberry API";
                    }
                }
                catch
                {
                    message = "Unknown error";
                }

                throw new Exception(message);
            }
        }

        private class ExceptionResponse
        {
            [JsonProperty("message")]
            public string Message { get; set; }
        }
    }
}
