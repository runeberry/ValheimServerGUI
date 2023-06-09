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
    public interface IXboxApiClient
    {
        Task<PlayerInfoResponse> GetPlayerSummary(string xuid);
    }

    /// <summary>
    /// Client for interacting with the OpenXBL API.
    /// </summary>
    /// <remarks>
    /// Reference the Swagger page at: https://xbl.io/console
    /// </remarks>
    public class XboxApiClient : RestClient, IXboxApiClient
    {
        public XboxApiClient(IRestClientContext context) : base(context)
        {
        }

        public async Task<PlayerInfoResponse> GetPlayerSummary(string xuid)
        {
            if (string.IsNullOrWhiteSpace(xuid))
            {
                throw new ArgumentNullException(nameof(xuid));
            }

            var response = await Get($"https://xbl.io/api/v2/player/summary/{xuid}")
                .WithHeader("x-authorization", ServerSecrets.XboxApiKey)
                .SendAsync<XboxPlayerSummaryResponse>();

            if (response == null)
            {
                throw new Exception($"Unsuccessful request with xuid: {xuid}");
            }

            if (response.People == null || !response.People.Any())
            {
                throw new Exception($"No players on response for xuid: {xuid}");
            }

            var person = response.People.FirstOrDefault(p => p.Xuid == xuid);
            if (person == null)
            {
                throw new Exception($"No players matching xuid on response for: {xuid}");
            }

            return new PlayerInfoResponse(PlayerPlatforms.Xbox, person.Xuid, person.DisplayName);
        }

        #region Nested symbols

        private class XboxPlayerSummaryResponse
        {
            [JsonProperty("people")]
            public List<Person> People { get; set; }

            public class Person
            {
                [JsonProperty("xuid")]
                public string Xuid { get; set; }

                [JsonProperty("displayName")]
                public string DisplayName { get; set; }
            }
        }

        #endregion
    }
}
