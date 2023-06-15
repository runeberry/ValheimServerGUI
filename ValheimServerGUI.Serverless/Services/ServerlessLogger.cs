using Amazon.Lambda.Core;
using Serilog;
using Serilog.Events;

namespace ValheimServerGUI.Serverless.Services
{
    public class ServerlessLogger : ILogger
    {
        public void Write(LogEvent logEvent)
        {
            LambdaLogger.Log(logEvent.RenderMessage());
        }
    }
}
