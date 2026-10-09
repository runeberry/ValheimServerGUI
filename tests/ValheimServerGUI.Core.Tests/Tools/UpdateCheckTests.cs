using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Serilog;
using ValheimServerGUI.Core.Tests.Fakes;
using ValheimServerGUI.Game;
using ValheimServerGUI.Localization;
using ValheimServerGUI.Properties;
using ValheimServerGUI.Tools;
using ValheimServerGUI.Tools.Http;
using ValheimServerGUI.Tools.Models;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Tools
{
    // The update check asks the app's API (/update-check), which picks the release server-side; the client only
    // interprets the answer: 200 = latest release, 404 = none qualifies, anything else (or an unusable 200) fails.
    public class UpdateCheckTests
    {
        private const string Release =
            "{\"version\":\"v2.4.0\",\"url\":\"https://forge.example/releases/tag/v2.4.0\",\"publishedAt\":\"2026-09-27T19:09:13Z\"}";

        private readonly StubHttpClientProvider _http = new();

        private RuneberryApiClient NewClient()
            => new(new RestClientContext(new LoggerConfiguration().CreateLogger(), _http));

        [Fact]
        public async Task Asks_the_api_update_check_route_with_the_client_key()
        {
            _http.RespondWith(HttpStatusCode.OK, Release);

            await NewClient().GetLatestReleaseAsync();

            var (request, _) = Assert.Single(_http.Requests);
            Assert.Equal($"{CoreConstants.UrlRuneberryApi}/update-check", request.RequestUri!.ToString());
            Assert.Equal(new[] { ClientSecrets.RuneberryClientApiKey }, request.Headers.GetValues(ClientSecrets.RuneberryApiKeyHeader));
        }

        [Fact]
        public async Task A_200_returns_the_version_and_release_page()
        {
            _http.RespondWith(HttpStatusCode.OK, Release);

            var release = await NewClient().GetLatestReleaseAsync();

            Assert.Equal("v2.4.0", release!.Version);
            Assert.Equal("https://forge.example/releases/tag/v2.4.0", release.Url);
        }

        [Fact]
        public async Task A_404_means_no_qualifying_release()
        {
            _http.RespondWith(HttpStatusCode.NotFound, "{\"message\":\"No release\"}");

            Assert.Null(await NewClient().GetLatestReleaseAsync());
        }

        [Theory]
        [InlineData(HttpStatusCode.BadGateway, "{\"message\":\"forge down\"}")]
        [InlineData(HttpStatusCode.Unauthorized, "{\"message\":\"bad key\"}")]
        [InlineData(HttpStatusCode.OK, "not json")]
        [InlineData(HttpStatusCode.OK, "{\"url\":\"https://forge.example/r\"}")]
        [InlineData(HttpStatusCode.OK, "{\"version\":\"not-a-version\",\"url\":\"https://forge.example/r\"}")]
        [InlineData(HttpStatusCode.OK, "{\"version\":\"v2.4.0\"}")]
        public async Task An_error_status_or_unusable_answer_fails_the_check(HttpStatusCode status, string body)
        {
            _http.RespondWith(status, body);

            var e = await Assert.ThrowsAsync<Exception>(() => NewClient().GetLatestReleaseAsync());
            Assert.Equal(Strings.Api_UpdateServerUnreachable, e.Message);
        }

        [Fact]
        public async Task An_unreachable_api_fails_the_check()
        {
            _http.Respond = _ => throw new System.Net.Http.HttpRequestException("no route");

            var e = await Assert.ThrowsAsync<Exception>(() => NewClient().GetLatestReleaseAsync());
            Assert.Equal(Strings.Api_UpdateServerUnreachable, e.Message);
        }

        // ---- SoftwareUpdateProvider ----

        private static SoftwareUpdateEventArgs Check(FakeRuneberryApiClient api)
        {
            var provider = new SoftwareUpdateProvider(api, new StubUserPreferencesProvider());
            SoftwareUpdateEventArgs? result = null;
            provider.UpdateCheckFinished += (_, e) => result = e;
            provider.CheckForUpdatesAsync(isManualCheck: true).GetAwaiter().GetResult();
            return result!;
        }

        [Fact]
        public void A_release_reports_its_version_and_page()
        {
            var result = Check(new FakeRuneberryApiClient { LatestRelease = new() { Version = "v9.9.9", Url = "https://forge.example/r" } });

            Assert.True(result.IsSuccessful);
            Assert.Equal("v9.9.9", result.LatestVersion);
            Assert.Equal("https://forge.example/r", result.ReleaseUrl);
        }

        [Fact]
        public void No_qualifying_release_reports_the_running_version_without_a_page()
        {
            var result = Check(new FakeRuneberryApiClient { LatestRelease = null });

            Assert.True(result.IsSuccessful);
            Assert.Equal(AssemblyHelper.GetApplicationVersion(), result.LatestVersion);
            Assert.Null(result.ReleaseUrl);
        }

        [Fact]
        public void A_failed_check_reports_the_error_without_a_page()
        {
            var result = Check(new FakeRuneberryApiClient { LatestReleaseError = new Exception("down") });

            Assert.False(result.IsSuccessful);
            Assert.Equal("down", result.Exception!.Message);
            Assert.Null(result.ReleaseUrl);
        }

        private sealed class StubUserPreferencesProvider : IUserPreferencesProvider
        {
            public event EventHandler<UserPreferences>? PreferencesSaved { add { } remove { } }
            public UserPreferences LoadPreferences() => new();
            public void SavePreferences(UserPreferences preferences) { }
        }
    }
}
