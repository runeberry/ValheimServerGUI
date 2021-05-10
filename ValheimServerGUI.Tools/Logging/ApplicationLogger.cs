using System.Diagnostics;

namespace ValheimServerGUI.Tools.Logging
{
    public class ApplicationLogger : EventLogger
    {
        public ApplicationLogger()
        {
            this.CategoryName = "Application";

            this.LogReceived += this.OnLogReceived;
        }

        private void OnLogReceived(object sender, EventLogContext context)
        {
            Debug.WriteLine($"[{context.Timestamp:T}] {context.Message}");
        }
    }
}
