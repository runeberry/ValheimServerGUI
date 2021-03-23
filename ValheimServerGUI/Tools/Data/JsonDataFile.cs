using Newtonsoft.Json;
using System.Collections.Generic;

namespace ValheimServerGUI.Tools.Data
{
    public class JsonDataFile<TEntity>
    {
        [JsonProperty("data")]
        public Dictionary<string, TEntity> Data { get; set; }
    }
}
