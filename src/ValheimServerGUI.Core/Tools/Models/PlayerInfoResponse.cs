using Newtonsoft.Json;

namespace ValheimServerGUI.Tools.Models
{
    public class PlayerInfoResponse
    {
        public PlayerInfoResponse(string platform, string id, string name)
        {
            Platform = platform;
            Id = id;
            Name = name;
        }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("platform")]
        public string Platform { get; set; }
    }
}
