using Newtonsoft.Json;

namespace ValheimServerGUI.Tools.Models
{
    /// <summary>The API's <c>/update-check</c> answer: the latest release's tag name and its release page.</summary>
    public class LatestReleaseResponse
    {
        [JsonProperty("version")]
        public string? Version { get; set; }

        [JsonProperty("url")]
        public string? Url { get; set; }
    }
}
