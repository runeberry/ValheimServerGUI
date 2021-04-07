using System.Net.Http;
using ValheimServerGUI.Tools.Logging;

namespace ValheimServerGUI.Tools
{
    public delegate void KeyValueEventHandler(object sender, string key, string value);
    public delegate void LogEventHandler(object sender, EventLogContext logEvent, string[] captures);

    public delegate void HttpResponseHandler(object sender, HttpResponseMessage message);
    public delegate void HttpResponseHandler<T>(object sender, T payload);
}
