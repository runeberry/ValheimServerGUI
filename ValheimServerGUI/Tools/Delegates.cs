namespace ValheimServerGUI.Tools
{
    public delegate void KeyValueEventHandler(object sender, string key, string value);
    public delegate void LogEventHandler(object sender, LogEvent logEvent, string[] captures);
}
