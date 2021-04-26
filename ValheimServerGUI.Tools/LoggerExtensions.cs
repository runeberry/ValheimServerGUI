using Microsoft.Extensions.Logging;
using System;

namespace ValheimServerGUI.Tools
{
    public static class LoggerExtensions
    {
        public static void LogException(this ILogger logger, Exception e, string message = null)
        {
            if (message != null) logger.LogError(message);
            logger.LogError($"{e.GetType().Name}: {e.Message}");
            logger.LogError(e.StackTrace);
        }
    }
}
