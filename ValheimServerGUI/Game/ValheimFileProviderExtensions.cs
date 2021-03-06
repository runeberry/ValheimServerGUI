using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValheimServerGUI.Game
{
    public static class ValheimFileProviderExtensions
    {
        public static List<string> GetWorldNames(this IValheimFileProvider files)
        {
            return files.WorldsFolder
                .GetFiles("*.db")
                .Select(f => Path.GetFileNameWithoutExtension(f.FullName))
                .ToList();
        }
    }
}
