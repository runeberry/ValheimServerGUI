using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ValheimServerGUI.Tools.Http;

namespace ValheimServerGUI.Tools
{
    public interface IGitHubClient
    {
        Task<GitHubRelease?> GetLatestReleaseAsync();
    }

    public class GitHubClient : RestClient, IGitHubClient
    {
        public GitHubClient(IRestClientContext context) : base(context)
        {
        }

        public async Task<GitHubRelease?> GetLatestReleaseAsync()
        {
            var releases = await Get($"{CoreConstants.UrlGithubApi}/releases")
                .WithHeader("User-Agent", "ValheimServerGUI")
                .SendAsync<GitHubRelease[]>();

            if (releases == null)
            {
                throw new Exception("Unable to reach GitHub.");
            }

            return SelectLatestRelease(releases);
        }

        /// <summary>
        /// Selects the release the app treats as "latest" for update checks: the most recently
        /// published release that has at least one asset and is neither a draft nor flagged as a
        /// GitHub pre-release. The pre-release exclusion is by GitHub's own flag, not the version
        /// string -- a pre-release version (e.g. 2.4.0-rc.1) published as an ordinary
        /// (non-pre-release) release is deliberately eligible, so that update checks surface it.
        /// Returns null if no release qualifies.
        /// </summary>
        public static GitHubRelease? SelectLatestRelease(IEnumerable<GitHubRelease> releases)
        {
            return releases
                .Where(r => r.Assets != null && r.Assets.Any())
                .Where(r => !r.Prerelease && !r.Draft)
                .OrderByDescending(r => r.PublishedAt)
                .FirstOrDefault();
        }
    }

    public class GitHubRelease
    {
        [JsonProperty("assets")]
        public object[]? Assets { get; set; }

        [JsonProperty("body")]
        public string? Body { get; set; }

        [JsonProperty("draft")]
        public bool Draft { get; set; }

        [JsonProperty("html_url")]
        public string? HtmlUrl { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("prerelease")]
        public bool Prerelease { get; set; }

        [JsonProperty("published_at")]
        public DateTime PublishedAt { get; set; }

        [JsonProperty("tag_name")]
        public string? TagName { get; set; }
    }
}
