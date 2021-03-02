using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ValheimServerGUI.Game
{
    public static class ValheimData
    {
        public static List<string> GetWorldNames(string worldsPath)
        {
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
