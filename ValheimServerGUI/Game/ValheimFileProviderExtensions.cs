using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ValheimServerGUI.Game
{
    public static class ValheimFileProviderExtensions
    {
        public static List<string> GetWorldNames(this IValheimFileProvider files)
        {
            return files.WorldsFolder
                .GetFiles("*.fwl")
                .Select(f => Path.GetFileNameWithoutExtension(f.FullName))
                .ToList();
        }

        public static bool IsWorldNameAvailable(this IValheimFileProvider files, string worldName)
        {
            if (string.IsNullOrWhiteSpace(worldName)) return false;

            var path = Path.Join(files.WorldsFolder.FullName, $"{worldName}.fwl");
            return !File.Exists(path);
        }
    }
}
