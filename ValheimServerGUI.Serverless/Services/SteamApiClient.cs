using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ValheimServerGUI.Properties;
using ValheimServerGUI.Tools.Http;
using ValheimServerGUI.Tools.Models;

namespace ValheimServerGUI.Serverless.Services
{
    public interface ISteamApiClient
    {
        Task<PlayerInfoResponse> GetPlayerSummary(string steamId);
    }

    public class SteamApiClient : RestClient, ISteamApiClient
    {
        public SteamApiClient(IRestClientContext context) : base(context)
        {
        }

        public async Task<PlayerInfoResponse> GetPlayerSummary(string steamId)
        {
            if (string.IsNullOrWhiteSpace(steamId))
            {
                throw new ArgumentNullException(nameof(steamId));
            }

            var response = await Get($"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?steamids={steamId}")
                .WithHeader("x-webapi-key", Secrets.SteamApiKey)
                .SendAsync<SteamPlayerSummaryResponse>();

            if (response == null)
            {
                throw new Exception($"Unsuccessful request with steamId: {steamId}");
            }

            if (response.Response == null || response.Response.Players == null || !response.Response.Players.Any())
            {
                throw new Exception($"No players on response for steamId: {steamId}");
            }

            var person = response.Response.Players.FirstOrDefault(p => p.SteamId == steamId);
            if (person == null)
            {
                throw new Exception($"No players matching steamId on response for: {steamId}");
            }

            return new PlayerInfoResponse(PlayerPlatforms.Steam, person.SteamId, person.PersonaName);
        }

        #region Nested symbols

        private class SteamPlayerSummaryResponse
        {
            [JsonProperty("response")]
            public ResponseObject Response { get; set; }

            public class ResponseObject
            {
                [JsonProperty("players")]
                public List<Player> Players { get; set; }

                public class Player
                {
                    [JsonProperty("steamid")]
                    public string SteamId { get; set; }

                    [JsonProperty("personaname")]
                    public string PersonaName { get; set; }
                }
            }
        }

        #endregion
    }
}
