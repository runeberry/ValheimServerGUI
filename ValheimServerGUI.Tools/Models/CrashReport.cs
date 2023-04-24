using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ValheimServerGUI.Tools.Models
{
    public class CrashReport
    {
        [JsonProperty("id")]
        public string CrashReportId { get; set; }

        [JsonProperty("clientCorrelationId")]
        public string ClientCorrelationId { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("timestamp")]
        public DateTimeOffset? Timestamp { get; set; }

        [JsonProperty("appVersion")]
        public string AppVersion { get; set; }

        [JsonProperty("osVersion")]
        public string OsVersion { get; set; }

        [JsonProperty("dotnetVersion")]
        public string DotnetVersion { get; set; }

        [JsonProperty("currentCulture")]
        public string CurrentCulture { get; set; }

        [JsonProperty("currentUiCulture")]
        public string CurrentUICulture { get; set; }

        [JsonProperty("additionalInfo")]
        public Dictionary<string, string> AdditionalInfo { get; set; }

        [JsonProperty("logs")]
        public List<string> Logs { get; set; }
    }
}
