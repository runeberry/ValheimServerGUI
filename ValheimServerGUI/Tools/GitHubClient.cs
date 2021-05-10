using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using ValheimServerGUI.Properties;
using ValheimServerGUI.Tools.Http;

namespace ValheimServerGUI.Tools
{
    public interface IGitHubClient
    {
        Task<GitHubRelease> GetLatestReleaseAsync();
    }

    public class GitHubClient : RestClient, IGitHubClient
    {
        public GitHubClient(IRestClientContext context) : base(context)
        {
        }

        public async Task<GitHubRelease> GetLatestReleaseAsync()
        {
            var releases = await this.Get($"{Resources.UrlGithubApi}/releases")
                .WithHeader("User-Agent", "ValheimServerGUI")
                .SendAsync<GitHubRelease[]>();

            if (releases == null)
            {
                throw new Exception("Unable to reach GitHub.");
            }

            var latestRelease = releases
                .Where(r => r.Assets != null && r.Assets.Any())
                .Where(r => !r.Prerelease && !r.Draft)
                .OrderByDescending(r => r.PublishedAt)
                .FirstOrDefault();

            return latestRelease;
        }
    }

    public class GitHubRelease
    {
        [JsonProperty("assets")]
        public object[] Assets { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }

        [JsonProperty("draft")]
        public bool Draft { get; set; }

        [JsonProperty("html_url")]
        public string HtmlUrl { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("prerelease")]
        public bool Prerelease { get; set; }
        
        [JsonProperty("published_at")]
        public DateTime PublishedAt { get; set; }

        [JsonProperty("tag_name")]
        public string TagName { get; set; }
    }
}
