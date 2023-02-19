using System.Diagnostics;

namespace ValheimServerGUI.Tools.Logging
{
    public class ApplicationLogger : EventLogger
    {
        public ApplicationLogger()
        {
            CategoryName = "Application";

            LogReceived += OnLogReceived;
        }

        private void OnLogReceived(object sender, EventLogContext context)
        {
            Debug.WriteLine($"[{context.Timestamp:T}] {context.Message}");
        }
    }
}
