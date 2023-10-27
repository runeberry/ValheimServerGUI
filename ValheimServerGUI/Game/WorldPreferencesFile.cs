using Newtonsoft.Json;
using System.Collections.Generic;

namespace ValheimServerGUI.Game
{
    public class WorldPreferencesFile
    {
        [JsonProperty("worldName")]
        public string WorldName { get; set; }

        [JsonProperty("preset")]
        public string Preset { get; set; }

        [JsonProperty("modifiers")]
        public Dictionary<string, string> Modifiers { get; set; }

        [JsonProperty("keys")]
        public List<string> Keys { get; set; }
    }
}
