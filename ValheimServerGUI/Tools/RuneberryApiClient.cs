using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using ValheimServerGUI.Properties;
using ValheimServerGUI.Tools.Http;
using ValheimServerGUI.Tools.Models;

namespace ValheimServerGUI.Tools
{
    public interface IRuneberryApiClient
    {
        event EventHandler<PlayerInfoResponse> PlayerInfoAvailable;

        Task RequestPlayerInfoAsync(string platform, string playerId);

        Task SendCrashReportAsync(CrashReport report);
    }

    public class RuneberryApiClient : RestClient, IRuneberryApiClient
    {
        public RuneberryApiClient(IRestClientContext context) : base(context)
        {
        }

        #region IRuneberryApiClient implementation

        public event EventHandler<PlayerInfoResponse> PlayerInfoAvailable;

        public async Task RequestPlayerInfoAsync(string platform, string playerId)
        {
            var response = await Get($"{Resources.UrlRuneberryApi}/player-info?platform={platform}&playerId={playerId}")
                .WithHeader(ClientSecrets.RuneberryApiKeyHeader, ClientSecrets.RuneberryClientApiKey)
                .SendAsync<PlayerInfoResponse>();

            if (response == null)
            {
                Logger.Error($"Unable to get info for {platform} player with ID {playerId}");
                return;
            }

            PlayerInfoAvailable?.Invoke(this, response);
        }

        public async Task SendCrashReportAsync(CrashReport report)
        {
            var response = await Post($"{Resources.UrlRuneberryApi}/crash-report", report)
                .WithHeader(ClientSecrets.RuneberryApiKeyHeader, ClientSecrets.RuneberryClientApiKey)
                .SendAsync();

            if (response == null || !response.IsSuccessStatusCode)
            {
                string message;

                try
                {
                    if (response != null)
                    {
                        var rawResponse = await response.Content.ReadAsStringAsync();
                        var exceptionResponse = JsonConvert.DeserializeObject<ErrorResponse>(rawResponse);
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

        #endregion
    }
}
