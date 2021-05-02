using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ValheimServerGUI.Game
{
    public static class ValheimFileProviderExtensions
    {
        public static List<string> GetWorldNames(this IValheimFileProvider files)
        {
            try
            {
                return files.WorldsFolder
                    .GetFiles("*.fwl")
                    .Select(f => Path.GetFileNameWithoutExtension(f.FullName))
                    .ToList();
            }
            catch
            {
                // Return an empty list of names if we cannot load the worlds folder
                return new();
            }
        }

        public static bool IsWorldNameAvailable(this IValheimFileProvider files, string worldName)
        {
            if (string.IsNullOrWhiteSpace(worldName)) return false;

            try
            {
                var path = Path.Join(files.WorldsFolder.FullName, $"{worldName}.fwl");
                return !File.Exists(path);
            }
            catch
            {
                // Assume the world name is available if we cannot load the worlds folder
                return true;
            }
        }
    }
}
