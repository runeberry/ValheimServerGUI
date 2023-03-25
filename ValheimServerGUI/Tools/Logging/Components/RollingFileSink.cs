using Serilog;
using System;
using System.IO;

namespace ValheimServerGUI.Tools.Logging.Components
{
    public static class RollingFileSinkExtensions
    {
        private const string DefaultOutputTemplate = "{Message:lj}{NewLine}";

        public static LoggerConfiguration WriteToRollingFile(
            this LoggerConfiguration config,
            string directory,
            string filename)
        {
            filename = PathExtensions.GetValidFileName(filename);
            directory = Environment.ExpandEnvironmentVariables(directory);
            var filepath = Path.Join(directory, $"{filename}_.txt");

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return config.WriteTo.File(filepath,
                rollingInterval: RollingInterval.Day,
                outputTemplate: DefaultOutputTemplate);
        }
    }
}
