using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValheimServerGUI.Game
{
    public static class ValheimData
    {
        public const string GameDirectory = @"C:\Program Files (x86)\Steam\steamapps\common\Valheim";

        public const string GameFileName = "valheim.exe";

        public const string DataDirectory = @"%USERPROFILE%\AppData\LocalLow\IronGate\Valheim";

        public const string WorldsFolder = "worlds";

        public static List<string> GetWorldNames()
        {
            var dataFolder = Environment.ExpandEnvironmentVariables(DataDirectory);
            var worldsPath = Path.Combine(dataFolder, WorldsFolder);

            if (!Directory.Exists(worldsPath))
            {
                throw new DirectoryNotFoundException($"Valheim worlds directory does not exists at: {worldsPath}");
            }

            return Directory.GetFiles(worldsPath)
                .Where(path => path.EndsWith(".db"))
                .Select(path => Path.GetFileNameWithoutExtension(path))
                .ToList();
        }
    }
}
