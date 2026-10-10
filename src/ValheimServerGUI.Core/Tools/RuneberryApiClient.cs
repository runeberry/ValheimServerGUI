using Newtonsoft.Json;
using Semver;
using System;
using System.Net;
using System.Threading.Tasks;
using ValheimServerGUI.Localization;
using ValheimServerGUI.Properties;
using ValheimServerGUI.Tools.Http;
using ValheimServerGUI.Tools.Models;

namespace ValheimServerGUI.Tools
{
    public interface IRuneberryApiClient
    {
        Task SendCrashReportAsync(CrashReport report);

        /// <summary>
        /// The latest release eligible for update notices (the API picks it: newest published, non-draft,
        /// non-pre-release release with an asset). Null when no release qualifies. Throws when the API can't be
        /// reached, answers with an error, or returns a response without a parseable version and a URL.
        /// </summary>
        Task<LatestReleaseResponse?> GetLatestReleaseAsync();
    }

    public class RuneberryApiClient : RestClient, IRuneberryApiClient
    {
        public RuneberryApiClient(IRestClientContext context) : base(context)
        {
        }

        #region IRuneberryApiClient implementation

        public async Task SendCrashReportAsync(CrashReport report)
        {
            var response = await Post($"{CoreConstants.UrlRuneberryApi}/crash-report", report)
                .WithRuneberryApiKey()
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
                        message = $"({(int)response.StatusCode}) {exceptionResponse?.Message}";
                    }
                    else
                    {
                        message = Strings.Api_RuneberryUnreachable;
                    }
                }
                catch
                {
                    message = Strings.Api_UnknownError;
                }

                throw new Exception(message);
            }
        }

        public async Task<LatestReleaseResponse?> GetLatestReleaseAsync()
        {
            var response = await Get($"{CoreConstants.UrlRuneberryApi}/update-check")
                .WithRuneberryApiKey()
                .SendAsync();

            if (response?.StatusCode == HttpStatusCode.NotFound) return null;
            if (response == null || !response.IsSuccessStatusCode) throw new Exception(Strings.Api_UpdateServerUnreachable);

            LatestReleaseResponse? release;
            try
            {
                release = JsonConvert.DeserializeObject<LatestReleaseResponse>(await response.Content.ReadAsStringAsync());
            }
            catch (JsonException)
            {
                release = null;
            }

            // An unusable answer fails the check the same way an error status does.
            if (release == null
                || string.IsNullOrWhiteSpace(release.Url)
                || !SemVersion.TryParse(release.Version, SemVersionStyles.Any, out _))
            {
                Logger.Error("Update check returned an unusable response: {0}", CoreConstants.UrlRuneberryApi + "/update-check");
                throw new Exception(Strings.Api_UpdateServerUnreachable);
            }

            return release;
        }

        #endregion
    }

    internal static class RuneberryApiRequestExtensions
    {
        /// <summary>Attaches the client API key every Runeberry API route requires.</summary>
        public static RestClientRequest WithRuneberryApiKey(this RestClientRequest request)
            => request.WithHeader(ClientSecrets.RuneberryApiKeyHeader, ClientSecrets.RuneberryClientApiKey);
    }
}
