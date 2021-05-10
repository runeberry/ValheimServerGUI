using ValheimServerGUI.Tools.Logging;

namespace ValheimServerGUI.Tools
{
    public delegate void KeyValueEventHandler(object sender, string key, string value);
    public delegate void LogEventHandler(object sender, EventLogContext logEvent, string[] captures);
}
