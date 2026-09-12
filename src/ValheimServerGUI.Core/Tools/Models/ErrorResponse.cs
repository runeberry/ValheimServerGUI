using Newtonsoft.Json;

namespace ValheimServerGUI.Tools.Models
{
    public class ErrorResponse
    {
        public ErrorResponse(string message)
        {
            Message = message;
        }

        [JsonProperty("message")]
        public string Message { get; }
    }
}
