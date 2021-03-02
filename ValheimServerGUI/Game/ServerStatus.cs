namespace ValheimServerGUI.Game
{
    public enum ServerStatus
    {
        StartRequested = 10,
        Started = 11,

        StopRequested = 90,
        Stopped = 91,

        Error = 98,
        ProcessExited = 99,
    }
}
