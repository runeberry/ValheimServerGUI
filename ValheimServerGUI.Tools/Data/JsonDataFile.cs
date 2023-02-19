using Newtonsoft.Json;
using System.Collections.Generic;

namespace ValheimServerGUI.Tools.Data
{
    public class JsonDataFile<TEntity>
    {
        public JsonDataFile()
        {
        }

        public JsonDataFile(Dictionary<string, TEntity> data)
        {
            Data = data;
        }

        [JsonProperty("data")]
        public Dictionary<string, TEntity> Data { get; set; }
    }
}
